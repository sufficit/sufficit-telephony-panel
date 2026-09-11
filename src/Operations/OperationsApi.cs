using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Monitor;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class OperationsApi(IHttpClientFactory clients)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };
    private async Task<JsonElement> Send(string token, string path, CancellationToken ct, object? body = null)
    {
        using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, "https://endpoints.sufficit.com.br/" + path);
        request.Headers.Authorization = new("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await clients.CreateClient("operations").SendAsync(request, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("A API não autorizou esta conta para os dados telefônicos deste cliente. Verifique os direitos de telefonia na Identity.");
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return document.RootElement.Clone();
    }
    public async Task<EventsPanelCardInfo[]> Cards(string token, Guid? context, CancellationToken ct)
    {
        var json = await Send(token, context.HasValue ? $"telephony/eventspanel/cardsbycontext?contextid={context:D}" : "telephony/eventspanel/cardsbyuser", ct);
        if (json.ValueKind == JsonValueKind.Undefined) return [];
        var cards = json.Deserialize<EventsPanelCardInfo[]>(Json) ?? [];
        if (cards.Length > 2000) throw new InvalidOperationException("Há mais de 2.000 recursos. Selecione um cliente para reduzir a consulta.");
        if (cards.Any(c => c.Channels is null || c.Channels.Count > 64 || c.Label is null || c.Label.Length > 256 || c.Channels.Any(p => p is null || p.Length > 256)))
            throw new InvalidOperationException("O cadastro de recursos excede os limites de segurança do painel. Revise os identificadores no portal.");
        return cards;
    }
    public Task<JsonElement> Search(string token, string text, CancellationToken ct) =>
        Send(token, "contact/search?results=20&filter=" + Uri.EscapeDataString(text[..Math.Min(text.Length, 100)]), ct);
    public async Task<(Guid Id, string Title)?> Supervisor(string token, CancellationToken ct)
    {
        // Preferred is read-only. Unlike Config it does not auto-create a preference.
        var json = await Send(token, "telephony/chromeextension/preferred", ct);
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("id", out var id) || !id.TryGetGuid(out var guid)) return null;
        return (guid, json.TryGetProperty("title", out var title) ? title.GetString() ?? guid.ToString("N") : guid.ToString("N"));
    }
    public async Task Action(string token, TelephonyMonitorActionRequest request, CancellationToken ct)
    {
        var json = await Send(token, "telephony/monitor/actions", ct, request);
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("accepted", out var success) || success.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException("A API não confirmou o início do monitoramento.");
    }
}
