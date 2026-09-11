using System.Net;
using System.Text.Json;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Monitor;

public static class OperationsChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = Guid.NewGuid();
        var projection = new LiveProjection();
        JsonElement Event(string channel) => JsonSerializer.SerializeToElement(new { channel, accountcode = tenant.ToString("N"), channelstate = "Up", password = "must-not-be-retained" });
        projection.Apply("eveo", "NewChannelEvent", Event("PJSIP/6007-000000af"), now);
        projection.Apply("google", "NewChannelEvent", Event("PJSIP/6007-000000af"), now);
        var row = projection.Snapshot(now)[0];
        check(projection.Snapshot(now).Length == 2, "Same channel on different nodes remains isolated");
        check(!row.Details.ContainsKey("password"), "Projection cannot retain arbitrary sensitive fields");
        check(MonitorActionGuard.Allows(row, tenant, true, now), "Recent observed channel in same tenant may be monitored");
        check(!MonitorActionGuard.Allows(row, Guid.NewGuid(), true, now), "Cross-tenant monitoring rejected");
        check(!MonitorActionGuard.Allows(row, tenant, false, now), "Disconnected stream cannot authorize an action");
        check(!MonitorActionGuard.Allows(row, tenant, true, now.AddSeconds(31)), "Stale channel cannot authorize an action");
        check(!MonitorActionGuard.Allows(row with { Key = "PJSIP/6007\r\nAction: Hangup" }, tenant, true, now), "Protocol injection rejected");
        projection.Apply("eveo", "HangupEvent", Event(row.Key), now);
        check(projection.Snapshot(now).Single().Node == "google", "Hangup removes only its node");
        projection.Apply("eveo", "UnknownEvent", Event("bad"), now);
        check(projection.Snapshot(now).Length == 1, "Unregistered events ignored");
        var card = new EventsPanelCardInfo { Channels = ["^PJSIP/6007(?:-|$)"] };
        check(LiveProjection.Matches(card, row), "Existing card regex contract retained");
        card = new EventsPanelCardInfo { Channels = ["["] };
        check(!LiveProjection.Matches(card, row), "Malformed regex cannot break the stream");
        var authorized = new EventsPanelCardInfo { Channels = [".*"] };
        check(AuthorizedObservation.Create([authorized], [row], Guid.NewGuid()).Rows.Length == 0, "Broad card pattern cannot leak cross-tenant calls");
        check(AuthorizedObservation.Create([authorized], [row], tenant).Rows.Length == 1, "Same-tenant authorized call visible");
        var central = AuthorizedObservation.ForManager([row, row with { Id = "other", Account = Guid.NewGuid().ToString("N"), Key = "PJSIP/2001-000000ab" }]);
        check(central.Rows.Length == 2 && central.Cards.Length == 2, "Manager central view has no mandatory tenant or configured card");
        var local1 = row with { Id = "local1", Key = "Local/0000006001@sufficit-pjsip-000000ab;1",
            Details = new Dictionary<string, string> { ["linkedid"] = "linked-test" } };
        var local2 = local1 with { Id = "local2", Key = "Local/0000006001@sufficit-pjsip-000000ab;2" };
        var endpointCall = row with { Details = local1.Details };
        var withLocal = AuthorizedObservation.ForManager([endpointCall, local1, local2]);
        check(withLocal.Cards.Length == 1 && withLocal.Cards.Single().Channels.Single() == "PJSIP/6007",
            "Internal Local legs do not create extension cards");
        check(withLocal.Rows.Length == 3 && withLocal.Rows.Count(r => r.Key.StartsWith("Local/")) == 2,
            "Local channels remain in diagnostic rows");
        check(ObservedCallGroup.Create(withLocal.Rows).Single().Channels.Length == 3,
            "Removing Local cards preserves all legs of the correlated call");
        var endpointPeer = row with { Id = "peer", Kind = "peer", Key = "PJSIP/6007" };
        var merged = AuthorizedObservation.ForManager([endpointCall, endpointPeer, local1]);
        check(merged.Cards.Length == 1 && merged.ByCard.Values.Single().Count == 2,
            "Endpoint call and peer event share one card without Local legs");
        check(AuthorizedObservation.ForManager([local1, local2]).Cards.Length == 0,
            "Local-only activity cannot manufacture extensions");
        check(AuthorizedObservation.ForManager([row with { Key = "Bridge/internal" }]).Cards.Length == 0,
            "Other non-endpoint channels are diagnostic-only");
        check(AuthorizedObservation.ForManager([row with { Key = "SIP/in-dtr-abcdefgh" }]).Cards.Single().Label == "SIP/in-dtr-abcdefgh",
            "Nonhex endpoint suffix is not truncated");
        check(AuthorizedObservation.ForManager([row with { Key = "IAX2/peer-000000ab" }]).Cards.Single().Label == "IAX2/peer",
            "Known network endpoint technology remains supported");
        check(AuthorizedObservation.Create(central.Cards, central.Rows, tenant).Rows.Length == 1, "Switching to company restores tenant isolation");
        check(!MonitorActionGuard.Allows(row with { Account = null }, Guid.Empty, true, now), "Global read scope never grants a tenantless action");
        projection.Apply("eveo", "ContactStatusEvent", JsonSerializer.SerializeToElement(new { endpointname = "6007", contactstatus = "Reachable" }), now);
        check(projection.Snapshot(now).Any(r => r.Key == "PJSIP/6007"), "PJSIP contacts normalized to the card identifier");
        projection.Apply("eveo", "PeerStatusEvent", JsonSerializer.SerializeToElement(new { peer = "PJSIP/6007", peerstatus = 6 }), now);
        check(projection.Snapshot(now).Single(r => r.Key == "PJSIP/6007").State == "Registered", "Peer enum must not be interpreted as a channel enum");
        var peer = projection.Snapshot(now).Single(r => r.Key == "PJSIP/6007").Peer!;
        check(peer.Registration == "registered" && peer.LastRegisteredObservedAt == now, "Only registration event sets last observed registration");
        projection.Apply("eveo", "ContactStatusEvent", JsonSerializer.SerializeToElement(new { endpointname = "6007", contactstatus = "Unreachable", roundtripusec = 15000 }), now.AddSeconds(5));
        peer = projection.Snapshot(now.AddSeconds(5)).Single(r => r.Key == "PJSIP/6007").Peer!;
        check(peer.Registration == "registered" && peer.Reachability == "offline" && peer.LastRegisteredObservedAt == now, "Failed qualify does not erase registration or change its timestamp");
        check(peer.RoundTripMilliseconds == 15, "Contact RTT uses milliseconds, never a registration date");
        projection.Apply("eveo", "DeviceStateChangeEvent", JsonSerializer.SerializeToElement(new { device = "PJSIP/6007", state = 6 }), now.AddSeconds(6));
        peer = projection.Snapshot(now.AddSeconds(6)).Single(r => r.Key == "PJSIP/6007").Peer!;
        check(peer.DeviceLabel == "Tocando" && peer.Reachability == "offline" && peer.Registration == "registered", "Device state enum is independent from reachability and registration");
        projection.Apply("eveo", "PeerStatusEvent", JsonSerializer.SerializeToElement(new { peer = "PJSIP/6007", peerstatus = "Unregistered" }), now.AddSeconds(7));
        peer = projection.Snapshot(now.AddSeconds(7)).Single(r => r.Key == "PJSIP/6007").Peer!;
        check(peer.Registration == "unregistered" && peer.LastRegisteredObservedAt == now, "Unregistration retains the last observed registration");
        check(new PeerEvidence().Apply("ContactStatusEvent", "Reachable", new Dictionary<string,string>(), now).Registration == "unknown", "Reachable static contact must not be presented as registered");
        projection.Clear();
        check(projection.Snapshot(now).Length == 0 && projection.LastEvent is null, "Reconnect clears the old projection");
        for (var i = 0; i < LiveProjection.Capacity + 1; i++) projection.Apply("eveo", "NewChannelEvent", Event("PJSIP/" + i), now);
        check(projection.Snapshot(now).Length == LiveProjection.Capacity && projection.Overflow, "Projection memory bounded");
        check(projection.Snapshot(now.AddHours(3)).Length == 0, "Unreconciled old resources expire");
        var http = new FakeHttp { Body = "{\"accepted\":true}" };
        var api = new OperationsApi(http);
        await api.Action("synthetic", new TelephonyMonitorActionRequest(), default);
        check(true, "Actual accepted response contract honored");
        http.Body = "{\"accepted\":false}";
        try { await api.Action("synthetic", new TelephonyMonitorActionRequest(), default); check(false, "Rejected action must not look successful"); }
        catch (InvalidOperationException) { check(true, "Rejected action reported"); }
        http.Status = HttpStatusCode.Forbidden;
        try { await api.Cards("synthetic", tenant, default); check(false, "403 must not look like empty cards"); }
        catch (UnauthorizedAccessException) { check(true, "Tenant permission failure explicit"); }
    }
}
