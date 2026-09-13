using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Sufficit.Telephony.Panel.Security;
using Sufficit.Telephony.Panel.Monitoring;

var count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
PanelTextChecks.Run(Check);
var clock = new FakeClock();
var http = new FakeHttp { Body = "{\"sub\":\"operator-a\",\"role\":[\"manager\"]}" };
using var sessions = new PanelSessions(http, clock);
AuthenticationTicket Ticket(string subject, DateTimeOffset? expiry = null)
{
    var principal = new ClaimsPrincipal(new ClaimsIdentity([new("sub", subject), new("role", "manager")], "panel", "name", "role"));
    var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
    properties.StoreTokens([new() { Name = "access_token", Value = "synthetic-not-a-real-token" }, new() { Name = "expires_at", Value = (expiry ?? clock.Now.AddHours(1)).ToString("O") }]);
    return new(principal, properties, "panel");
}
Check(!await sessions.IsManagerAsync(new ClaimsPrincipal(), default), "Anonymous must fail closed");
var ticket = Ticket("operator-a");
var key = await sessions.StoreAsync(ticket);
Check(key.Length == 32 && Guid.TryParseExact(key, "N", out _), "Session handles follow compact UUID format");
Check(await sessions.IsManagerAsync(ticket.Principal, default), "Current manager accepted");
await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => sessions.IsManagerAsync(ticket.Principal, default)));
Check(http.Requests == 1, "Concurrent cache hits do not fan out identity queries");
http.Body = "{\"sub\":\"operator-a\",\"role\":[\"user\"]}";
clock.Now = clock.Now.AddSeconds(61);
Check(!await sessions.IsManagerAsync(ticket.Principal, default), "Removed manager role rejected after bounded TTL");
Check(await sessions.RetrieveAsync(key) is null, "Revoked session evicted");
var wrong = Ticket("operator-b"); await sessions.StoreAsync(wrong);
http.Body = "{\"sub\":\"operator-a\",\"role\":[\"manager\"]}";
Check(!await sessions.IsManagerAsync(wrong.Principal, default), "Userinfo subject mismatch rejected");
var expired = Ticket("operator-a", clock.Now.AddSeconds(-1)); await sessions.StoreAsync(expired);
Check(!await sessions.IsManagerAsync(expired.Principal, default), "Expired token rejected independently of role cache");
var failure = Ticket("operator-a"); await sessions.StoreAsync(failure);
http.Status = System.Net.HttpStatusCode.ServiceUnavailable;
Check(!await sessions.IsManagerAsync(failure.Principal, default), "Identity failure cannot grant access");
http.Status = System.Net.HttpStatusCode.OK; http.Body = "not json";
Check(!await sessions.IsManagerAsync(failure.Principal, default), "Malformed identity response denied");
http.Body = "{\"sub\":\"operator-a\",\"role\":\"manager\"}";
Check(await sessions.IsManagerAsync(failure.Principal, default), "Scalar role works on recovery");
await sessions.RemoveAsync(failure.Principal.FindFirstValue("panel_session")!);
Check(!await sessions.IsManagerAsync(failure.Principal, default), "Logout invalidates existing circuit access");

var now = DateTimeOffset.UtcNow;
object Row(string name, string value, string node = "apoint-voip") => new { metric = new Dictionary<string,string> { ["__name__"] = name, ["node"] = node }, value = new object[] { now.ToUnixTimeSeconds(), value } };
FleetSnapshot Snapshot(params object[] rows)
{
    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { data = new { result = rows } }));
    return FleetMetrics.Parse(doc.RootElement, now);
}
var healthy = Row("sufficit_pbx_collection_healthy", "1");
var fresh = Row("sufficit_pbx_sample_timestamp_seconds", (now.ToUnixTimeSeconds() - 30).ToString());
var calls = Row("sufficit_pbx_active_calls", "0");
var valid = Snapshot(healthy, fresh, calls);
Check(valid.Nodes.Count == 3, "Expected nodes present even when series missing");
Check(valid.Nodes[0].Fresh && valid.Nodes[0].Calls == 0, "Observed zero remains zero");
Check(!valid.Nodes[1].Fresh && valid.Nodes[1].Calls is null, "Missing node not fabricated as zero");
Check(Snapshot(healthy, calls).Nodes[0].Calls is null, "No timestamp means no operational values");
Check(Snapshot(healthy, Row("sufficit_pbx_sample_timestamp_seconds", (now.ToUnixTimeSeconds()-141).ToString()), calls).Nodes[0].Calls is null, "Stale sample hidden");
Check(Snapshot(Row("sufficit_pbx_collection_healthy", "0"), fresh, calls).Nodes[0].Calls is null, "Failed collection hidden");
Check(Snapshot(healthy, fresh, Row("sufficit_pbx_active_calls", "NaN")).Nodes[0].Calls is null, "NaN hidden");
Check(Snapshot(healthy, fresh, calls, calls).Nodes[0].Calls is null, "Ambiguous duplicate series fail closed");
Check(Snapshot(healthy, fresh).Nodes[0].AmiUp is null, "AMI missing is not a disconnected assertion");
await OperationsChecks.Run(Check);
BoardSupervisionChecks.Run(Check);
await SharedTelemetryChecks.Run(Check);
CallGroupingChecks.Run(Check);
QueueCallFlowChecks.Run(Check);
await AcdBoardChecks.Run(Check);
OperatorBoardChecks.Run(Check);
await BoardConfigurationChecks.Run(Check);
FopRulesChecks.Run(Check);
await BoardInventoryChecks.Run(Check);
BoardViewQueryChecks.Run(Check);
Console.WriteLine($"PASS: {count} security, concurrency, revocation, projection and action checks.");
