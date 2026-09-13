using System.Net;
using System.Text.Json;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Bounded single-flight private snapshots. Authorization belongs to AcdBoardAccess.</summary>
public sealed class AcdBoardReader : IDisposable
{
    private sealed class Slot(AcdBoardSource source, Guid context)
    {
        public readonly AcdBoardSource Source = source;
        public readonly Guid Context = context;
        public readonly SemaphoreSlim Gate = new(1);
        public AcdBoardSample? Value;
        public DateTimeOffset Attempted;
    }
    private readonly Slot[] slots;
    private readonly IHttpClientFactory clients;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim requests = new(4);
    public bool Configured => slots.Length > 0;
    public AcdBoardReader(IEnumerable<AcdBoardSource> sources, IHttpClientFactory clients, TimeProvider clock)
    {
        this.clients = clients; this.clock = clock;
        var definitions = sources.ToArray();
        if (definitions.Length > 16 || definitions.Any(s => !ValidSource(s)) ||
            definitions.Sum(s => s.ContextIds.Length) > 64 ||
            definitions.SelectMany(s => s.ContextIds.Select(c => (s.Node, c))).Distinct().Count() != definitions.Sum(s => s.ContextIds.Length))
            throw new InvalidOperationException("Invalid private ACD board sources.");
        slots = definitions.SelectMany(s => s.ContextIds.Select(c => new Slot(s, c))).ToArray();
    }
    public static bool ValidSource(AcdBoardSource source)
    {
        if (source is null || source.NodeId == Guid.Empty || string.IsNullOrWhiteSpace(source.Node) || source.Node.Length > 64 ||
            !source.Node.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') || source.ContextIds is not { Length: > 0 and <= 64 } ||
            source.ContextIds.Any(c => c == Guid.Empty) || string.IsNullOrWhiteSpace(source.KeyFile) || !Path.IsPathFullyQualified(source.KeyFile) ||
            !Uri.TryCreate(source.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != "/" ||
            !IPAddress.TryParse(uri.Host, out var ip)) return false;
        var bytes = ip.GetAddressBytes();
        return IPAddress.IsLoopback(ip) || bytes.Length == 4 && bytes[0] == 172 && bytes[1] == 19;
    }
    public async Task<AcdBoardSample[]> Read(Guid? context, CancellationToken ct)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(5));
        return await Task.WhenAll(slots.Where(s => context is null || s.Context == context).Select(s => Read(s, budget.Token, ct)));
    }
    private async Task<AcdBoardSample> Read(Slot slot, CancellationToken budget, CancellationToken caller)
    {
        var locked = false; var sending = false;
        try
        {
            await slot.Gate.WaitAsync(budget); locked = true;
            if (slot.Value is not null && clock.GetUtcNow() - slot.Attempted < TimeSpan.FromSeconds(2)) return slot.Value;
            await requests.WaitAsync(budget); sending = true;
            slot.Attempted = clock.GetUtcNow();
            var info = new FileInfo(slot.Source.KeyFile);
            if (!info.Exists || info.Length is < 32 or > 4096) throw new InvalidDataException("Invalid ACD credential file.");
            if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(info.FullName) &
                (UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.GroupWrite)) != 0)
                throw new InvalidDataException("ACD credential file must be private.");
            var key = (await File.ReadAllTextAsync(info.FullName, budget)).Trim();
            if (key.Length is < 32 or > 256 || key.Any(char.IsWhiteSpace)) throw new InvalidDataException("Invalid ACD credential.");
            using var request = new HttpRequestMessage(HttpMethod.Get, slot.Source.BaseUrl.TrimEnd('/') +
                $"/api/acd/contexts/{slot.Context:N}/nodes/{slot.Source.NodeId:N}/snapshot");
            request.Headers.Authorization = new("Bearer", key);
            using var response = await clients.CreateClient("acd-board").SendAsync(request, budget);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return slot.Value = new(slot.Source.Node, slot.Context, null, false, slot.Source.NodeId);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(budget);
            if (json.Length > 4 * 1024 * 1024) throw new InvalidDataException("ACD snapshot exceeds its bound.");
            var snapshot = JsonSerializer.Deserialize<AcdBoardSnapshot>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (snapshot is null || !ValidSnapshot(snapshot, slot.Context, slot.Source.NodeId)) throw new InvalidDataException("Invalid ACD snapshot scope or shape.");
            return slot.Value = new(slot.Source.Node, slot.Context, snapshot, true, slot.Source.NodeId);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or JsonException or UnauthorizedAccessException or OperationCanceledException)
        {
            caller.ThrowIfCancellationRequested();
            var failed = new AcdBoardSample(slot.Source.Node, slot.Context, slot.Value?.Snapshot, false, slot.Source.NodeId);
            if (locked) { slot.Attempted = clock.GetUtcNow(); slot.Value = failed; }
            return failed;
        }
        finally { if (sending) requests.Release(); if (locked) slot.Gate.Release(); }
    }
    public static bool ValidSnapshot(AcdBoardSnapshot s, Guid context, Guid node)
    {
        static bool Id(string? value) => Guid.TryParseExact(value, "N", out var id) && id != Guid.Empty;
        static bool Text(string? value, int max) => value is null || value.Length <= max && !value.Any(char.IsControl);
        static bool Contacts(AcdContact[]? contacts) => contacts is null || contacts.Length <= 16 && contacts.All(c =>
            c is not null && Text(c.Endpoint, 256) && Text(c.UserAgent, 160) &&
            (c.IpAddress is null || IPAddress.TryParse(c.IpAddress, out _)) &&
            (c.ViaAddress is null || IPAddress.TryParse(c.ViaAddress, out _)));
        if (s.Agents is not null && (s.Agents.Length > 128 ||
            s.Agents.Select(a => a?.Extension).Distinct().Count() != s.Agents.Length ||
            s.Agents.Any(a => a is null || !Guid.TryParse(a.ContextId, out var scope) || scope != context ||
                string.IsNullOrWhiteSpace(a.Extension) || !Text(a.Extension, 128) || !Contacts(a.Contacts) ||
                a.RegistrationHistory is not null && (a.RegistrationHistory.Length > 20 || a.RegistrationHistory.Any(h =>
                    h is null || h.Event is not ("observed" or "registered" or "renewed" or "unregistered" or "changed" or "unknown") ||
                    !Contacts(h.Contacts)))))) return false;
        if (!Guid.TryParseExact(s.ContextId, "N", out var actualContext) || actualContext != context ||
            !Guid.TryParseExact(s.NodeId, "N", out var actualNode) || actualNode != node || s.Queues is null || s.Queues.Length > 2000 ||
            s.Queues.Any(q => q is null || !Id(q.QueueId) || string.IsNullOrWhiteSpace(q.Title) || !Text(q.Title, 256) ||
                q.Capacity is < 1 or > 10000 || q.Waiting is < 0 or > 10000 || q.Connected is < 0 or > 10000 ||
                q.Greeting < 0 || q.Offering < 0 || q.Announcing < 0 || q.Greeting > q.Waiting || q.Offering > q.Waiting || q.Announcing > q.Waiting) ||
            s.Queues.Select(q => q.QueueId).Distinct().Count() != s.Queues.Length) return false;
        return s.Visits is null || s.Visits.Length <= 5000 && s.Visits.Select(v => v?.Id).Distinct().Count() == s.Visits.Length &&
            s.Visits.All(v => v is not null && Id(v.Id) && s.Queues.Any(q => q.QueueId == v.QueueId) &&
                v.Phase is "greeting" or "waiting" or "announcing" or "offering" or "connected" &&
                Text(v.ChannelId, 256) && Text(v.ChannelName, 256) && Text(v.Caller, 128) && v.EnteredAt <= s.ObservedAt.AddSeconds(5));
    }
    public void Dispose() { foreach (var slot in slots) slot.Gate.Dispose(); requests.Dispose(); }
}
