using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sufficit.Telephony.Panel.Monitoring;

/// <summary>Fixed queries, fixed node set and shared bounded caches; never accepts arbitrary PromQL.</summary>
public sealed class FleetMetrics(IHttpClientFactory clients, IConfiguration configuration) : IDisposable
{
    public static readonly string[] NodeIds = ["apoint-voip", "eveo-voip", "google-voip"];
    private readonly SemaphoreSlim gate = new(1, 1);
    private FleetSnapshot? last;
    private readonly Dictionary<string, (DateTimeOffset At, HistorySeries Series)> histories = new();
    public async Task<FleetSnapshot> ReadAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (last is not null && DateTimeOffset.UtcNow - last.FetchedAt < TimeSpan.FromSeconds(15)) return last;
            try
            {
                using var json = await QueryAsync("query", "{__name__=~\"sufficit_pbx_.*|sufficit_ami_observation_up|ALERTS\",fleet=\"sufficit-pbx\"}", "", ct);
                last = Parse(json.RootElement, DateTimeOffset.UtcNow);
            }
            catch (Exception error) when (error is HttpRequestException or IOException or JsonException or OperationCanceledException or InvalidOperationException or KeyNotFoundException or ArgumentOutOfRangeException)
            {
                last = new(DateTimeOffset.UtcNow, NodeIds.Select(id => EmptyNode(id)).ToArray(), [],
                    "Não foi possível consultar a central de monitoramento. Os valores atuais estão indisponíveis; tente atualizar.");
            }
            return last;
        }
        finally { gate.Release(); }
    }
    public async Task<HistorySeries> HistoryAsync(string node, string metric, int hours, CancellationToken ct)
    {
        if (!NodeIds.Contains(node) || hours is not (1 or 6 or 24)
            || metric is not ("calls" or "memory" or "work")) throw new ArgumentException("Unsupported history selection.");
        var key = $"{node}/{metric}/{hours}";
        await gate.WaitAsync(ct);
        try
        {
            if (histories.TryGetValue(key, out var cached) && DateTimeOffset.UtcNow - cached.At < TimeSpan.FromSeconds(30)) return cached.Series;
            var name = metric switch { "calls" => "active_calls", "memory" => "process_resident_bytes", _ => "taskprocessor_queued" };
            var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // At most 361 points and 27 cache keys across every user and browser tab.
            var query = $"sufficit_pbx_{name}{{node=\"{node}\",fleet=\"sufficit-pbx\"}}";
            using var json = await QueryAsync("query_range", query, $"&start={end - hours * 3600}&end={end}&step={hours * 10}", ct);
            var result = json.RootElement.GetProperty("data").GetProperty("result");
            var points = new List<HistoryPoint>();
            if (result.GetArrayLength() > 0)
                foreach (var value in result[0].GetProperty("values").EnumerateArray())
                    if (double.TryParse(value[1].GetString(), CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                        points.Add(new(DateTimeOffset.FromUnixTimeSeconds((long)value[0].GetDouble()), number));
            var series = new HistorySeries(DateTimeOffset.FromUnixTimeSeconds(end - hours * 3600), DateTimeOffset.FromUnixTimeSeconds(end), points);
            histories[key] = (DateTimeOffset.UtcNow, series);
            return series;
        }
        finally { gate.Release(); }
    }
    private async Task<JsonDocument> QueryAsync(string method, string query, string suffix, CancellationToken ct)
    {
        var passwordFile = configuration["Metrics:PasswordFile"];
        if (string.IsNullOrEmpty(passwordFile)) throw new InvalidOperationException("Metrics credentials not configured.");
        var password = (await File.ReadAllTextAsync(passwordFile, ct)).Trim();
        var url = new Uri(new Uri(configuration["Metrics:BaseUrl"]!), $"api/v1/{method}?query={Uri.EscapeDataString(query)}{suffix}");
        if (url.Scheme != "https") throw new InvalidOperationException("Metrics require TLS.");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(configuration["Metrics:Username"] + ":" + password)));
        using var response = await clients.CreateClient("metrics").SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (document.RootElement.GetProperty("status").GetString() != "success") { document.Dispose(); throw new InvalidOperationException("Metrics query failed."); }
        return document;
    }
    public static FleetSnapshot Parse(JsonElement root, DateTimeOffset now)
    {
        var rows = root.GetProperty("data").GetProperty("result").EnumerateArray().ToArray();
        double? Read(string node, string name)
        {
            var candidates = rows.Where(row => Label(row, "node") == node && Label(row, "__name__") == name).ToArray();
            if (candidates.Length != 1) return null;
            var value = candidates[0].GetProperty("value");
            if (now.ToUnixTimeSeconds() - value[0].GetDouble() > 140) return null;
            return double.TryParse(value[1].GetString(), CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;
        }
        var nodes = NodeIds.Select(id =>
        {
            var timestamp = Read(id, "sufficit_pbx_sample_timestamp_seconds");
            var age = timestamp.HasValue ? now.ToUnixTimeSeconds() - timestamp.Value : (double?)null;
            var fresh = Read(id, "sufficit_pbx_collection_healthy") == 1 && age is >= 0 and <= 140;
            double? Value(string name) => fresh ? Read(id, "sufficit_pbx_" + name) : null;
            var ami = Read(id, "sufficit_ami_observation_up");
            return new NodeSnapshot(id, Name(id), fresh, ami.HasValue ? ami == 1 : null, age,
                Value("active_calls"), Value("active_channels"), Value("process_resident_bytes"), Value("process_threads"),
                Value("taskprocessor_queued"), Value("stasis_queued"), Value("taskprocessors_at_high_water"),
                Value("process_start_timestamp_seconds"), timestamp.HasValue ? DateTimeOffset.FromUnixTimeSeconds((long)timestamp.Value) : null);
        }).ToArray();
        var alerts = rows.Where(row => Label(row, "__name__") == "ALERTS"
            && row.GetProperty("value")[1].GetString() == "1"
            && now.ToUnixTimeSeconds() - row.GetProperty("value")[0].GetDouble() <= 140)
            .Select(row => new PanelAlert(Label(row, "alertname"), Label(row, "node"), Label(row, "alertstate"))).ToArray();
        return new(now, nodes, alerts, null);
    }
    private static string Label(JsonElement row, string label) => row.GetProperty("metric").TryGetProperty(label, out var v) ? v.GetString() ?? "" : "";
    private static string Name(string id) => id.Split('-')[0] switch { "apoint" => "Apoint", "eveo" => "Eveo", _ => "Google" };
    private static NodeSnapshot EmptyNode(string id) => new(id, Name(id), false, null, null, null, null, null, null, null, null, null, null, null);
    public void Dispose() => gate.Dispose();
}
