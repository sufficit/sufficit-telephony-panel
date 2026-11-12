using System.Net;
using System.Text.Json;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class QueueIdentityLookupChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var owner = Guid.NewGuid(); var id = Guid.NewGuid(); const string number = "0000006500";
        var http = new FakeHttp { Body = JsonSerializer.Serialize(new { queueId = id, contextId = owner, extension = number, native = true }) };
        var api = new OperationsApi(http);
        var result = await api.QueueIdentity("synthetic-token", number, owner, default);
        check(result is { Native: true } && result.QueueId == id && result.ContextId == owner, "Legacy number lookup obtains real catalogue GUID and company");
        http.Body = JsonSerializer.Serialize(new { queueId = id, contextId = owner, extension = number, native = false });
        result = await api.QueueIdentity("synthetic-token", number, null, default);
        check(result is { Native: false } && result.ManagementUrl("https://blazor.sufficit.com.br").EndsWith($"/queue?contextid={owner:N}"), "Legacy-only GUID cannot open a nonexistent native editor");
        foreach (var body in new[] {
            JsonSerializer.Serialize(new { queueId = id, contextId = Guid.NewGuid(), extension = number, native = true }),
            JsonSerializer.Serialize(new { queueId = id, contextId = owner, extension = "6500", native = true }),
            JsonSerializer.Serialize(new { queueId = Guid.Empty, contextId = owner, extension = number, native = true }),
            JsonSerializer.Serialize(new { queueId = id, contextId = owner, extension = number }) })
        {
            http.Body = body;
            try { await api.QueueIdentity("synthetic-token", number, owner, default); check(false, "Invalid identity response accepted"); }
            catch (InvalidDataException) { check(true, "Identity lookup rejects scope/number/shape mismatch"); }
        }
        http.Status = HttpStatusCode.NotFound;
        check(await api.QueueIdentity("synthetic-token", number, owner, default) is null, "Missing queue is explicit absence, not invented identity");
        http.Status = HttpStatusCode.Conflict;
        try { await api.QueueIdentity("synthetic-token", number, owner, default); check(false, "Ambiguous catalogue accepted"); }
        catch (HttpRequestException e) { check(e.StatusCode == HttpStatusCode.Conflict, "Ambiguity is preserved for company selection"); }
        var tile = new OperatorTile("old", "Different title", "google-voip", EventsPanelCardKind.QUEUE, []) { Keys = [number] };
        check(QueueResourceIdentity.LegacyExtension(tile) == number, "Legacy lookup uses exact resource number, never its display title");
        check(QueueResourceIdentity.LegacyExtension(tile with { Keys = ["^Local/0000006500$"] }) == number, "Exact portal queue pattern retains leading zeros");
        check(QueueResourceIdentity.LegacyExtension(tile with { Keys = ["^Local/000.*$"] }) is null, "Wildcard queue patterns never choose an arbitrary catalogue record");
    }
}
