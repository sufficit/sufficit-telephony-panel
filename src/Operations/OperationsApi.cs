using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sufficit.Telephony.Panel;
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
    public async Task<QueueResourceIdentity?> QueueIdentity(string token, string extension, Guid? context, CancellationToken ct)
    {
        if (extension is not { Length: > 0 and <= 20 } || !extension.All(char.IsAsciiDigit) || context == Guid.Empty)
            throw new InvalidOperationException("Invalid legacy queue identity request.");
        JsonElement json;
        try { json = await Send(token, "telephony/panel/queue-identity?extension=" + Uri.EscapeDataString(extension)
            + (context is { } owner ? $"&contextid={owner:N}" : ""), ct); }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound) { return null; }
        var result = json.Deserialize<QueueResourceIdentity>(Json);
        if (!json.TryGetProperty("native", out var native) || native.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || result is null || result.ContextId == Guid.Empty || result.QueueId == Guid.Empty || result.Extension != extension ||
            context is { } expected && result.ContextId != expected)
            throw new InvalidDataException("Queue catalogue identity scope mismatch.");
        return result;
    }
    public async Task<QueueAccounting> Accounting(string token,Guid context,string node,string recordId,CancellationToken ct)
    {
        if(context==Guid.Empty || !System.Text.RegularExpressions.Regex.IsMatch(recordId,"^[0-9]{9,12}\\.[0-9]{1,20}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(node,"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$"))throw new InvalidOperationException("This channel cannot be correlated to accounting.");
        var json=await Send(token,$"telephony/calls/accounting?contextid={context:D}&recordid={Uri.EscapeDataString(recordId)}&node={Uri.EscapeDataString(node)}",ct);
        var record=json.Deserialize<QueueAccounting>(Json)??throw new InvalidDataException("Invalid accounting response.");
        if(record.ContextId!=context || record.RecordId!=recordId || record.Node!=node ||
            record.Status is not ("not_observed" or "pending_debit" or "legacy_debit_unknown" or "not_reconciled" or "confirmed"))throw new InvalidDataException("Accounting scope mismatch.");
        return record;
    }
    public async Task<InboundCallSource[]> Inbound(string token, Guid context, CancellationToken ct)
    {
        if (context == Guid.Empty) throw new ArgumentException("Concrete context required.");
        var json = await Send(token, $"telephony/panel/inbound?contextid={context:D}", ct);
        var sources = json.Deserialize<InboundCallSource[]>(Json) ?? [];
        if (sources.Length > 16 || sources.Any(s => s is null || string.IsNullOrWhiteSpace(s.Node) || s.Node.Length > 128
            || s.Calls is null || s.Calls.Length > 100 || s.Calls.Any(c => c is null || c.ContextId != context || c.DidId == Guid.Empty && (c.ReceiverEndpointId is null || c.DidAnchor)
                || c.Node != s.Node || c.Channel is not { Length: > 0 and <= 256 } || c.Channel.Any(char.IsControl)
                || c.UniqueId is not { Length: > 0 and <= 128 } || c.LinkedId is not { Length: > 0 and <= 128 }
                || c.ReceiverEndpointId == Guid.Empty)))
            throw new InvalidDataException("Inbound snapshot ownership or shape mismatch.");
        return sources;
    }
    public async Task<EndpointRegistration[]> Registrations(string token, Guid? context, CancellationToken ct)
    {
        var json = await Send(token, "telephony/panel/endpoint-presence" + (context is { } id ? $"?contextid={id:D}" : ""), ct);
        var rows = json.Deserialize<EndpointRegistration[]>(Json) ?? [];
        if (rows.Length > 10000 || rows.Any(r => r.ContextId == Guid.Empty || r.EndpointId == Guid.Empty
            || context.HasValue && r.ContextId != context.Value || string.IsNullOrWhiteSpace(r.Node) || r.Node.Length > 128
            || r.Title is null || r.Title.Length > 256 || r.Label is null || r.Label.Length > 256))
            throw new InvalidDataException("PJSIP registration scope or shape mismatch.");
        return rows;
    }
    public async Task<EventsPanelCardInfo[]> Cards(string token, Guid? context, CancellationToken ct)
    {
        if (!context.HasValue || context == Guid.Empty) throw new InvalidOperationException("A concrete context is required for the card catalogue.");
        var json = await Send(token, $"telephony/panel/cards?contextid={context:D}", ct);
        if (json.ValueKind == JsonValueKind.Undefined) return [];
        var cards = json.Deserialize<EventsPanelCardInfo[]>(Json) ?? [];
        if (cards.Length > 2000) throw new InvalidOperationException("Há mais de 2.000 recursos. Selecione um cliente para reduzir a consulta.");
        if (cards.Any(c => c.Channels is null || c.Channels.Count > 64 || c.Label is null || c.Label.Length > 256 || c.Channels.Any(p => p is null || p.Length > 256)))
            throw new InvalidOperationException("O cadastro de recursos excede os limites de segurança do painel. Revise os identificadores no portal.");
        // The portal titles cards with the extension description, which may be empty.
        // Never render a blank card: fall back to the endpoint identifier the operator dials.
        foreach (var card in cards.Where(c => string.IsNullOrWhiteSpace(c.Label)))
            card.Label = FallbackLabel(card.Channels);
        return cards;
    }
    /// <summary>Card channels are anchored patterns such as <c>^SIP/0000286651</c>; show them without regex syntax.</summary>
    public static string FallbackLabel(IEnumerable<string> channels) =>
        channels.Select(c => c.TrimStart('^').TrimEnd('$')).FirstOrDefault(c => c.Length > 0) ?? "—";
    public Task<JsonElement> Search(string token, string text, CancellationToken ct) =>
        Send(token, "contact/search?results=20&filter=" + Uri.EscapeDataString(text[..Math.Min(text.Length, 100)]), ct);
    /// <summary>Company of the user's preferred supervision endpoint (the call to it is attributed there).</summary>
    public async Task<Guid?> SupervisorContext(string token, CancellationToken ct)
    {
        var json = await Send(token, "telephony/chromeextension/preferred", ct);
        if (json.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in new[] { "contextid", "contextId", "ContextId" }) // EndPoint serializes as "contextid"
            if (json.TryGetProperty(name, out var value) && value.TryGetGuid(out var id) && id != Guid.Empty) return id;
        return null;
    }
    public async Task<(Guid Id, string Title)?> Supervisor(string token, CancellationToken ct)
    {
        // Preferred is read-only. Unlike Config it does not auto-create a preference.
        var json = await Send(token, "telephony/chromeextension/preferred", ct);
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("id", out var id) || !id.TryGetGuid(out var guid)) return null;
        return (guid, json.TryGetProperty("title", out var title) ? title.GetString() ?? guid.ToString("N") : guid.ToString("N"));
    }
    public async Task Hangup(string token, ObservedResource target, CancellationToken ct)
    {
        var json=await Send(token,"telephony/panel/channels/hangup",ct,new {
            node=target.Node,channel=target.Key,uniqueId=target.Details.GetValueOrDefault("uniqueid") });
        if(json.ValueKind!=JsonValueKind.Object || !json.TryGetProperty("accepted",out var accepted) || accepted.ValueKind!=JsonValueKind.True)
            throw new InvalidOperationException("The PBX did not accept channel termination.");
    }
    public async Task Action(string token, TelephonyMonitorActionRequest request, CancellationToken ct)
    {
        var json = await Send(token, "telephony/monitor/actions", ct, request);
        if (json.ValueKind != JsonValueKind.Object || !json.TryGetProperty("accepted", out var success) || success.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException("A API não confirmou o início do monitoramento.");
    }
}
