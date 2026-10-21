using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.EndPoints.Controllers.Telephony;

internal static class CustomerAccessChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        check(CustomerAccessPreview.Read("https://example.test/board?preview=true").Manager, "Existing development preview stays manager by default");
        var a = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var b = Guid.Parse("22222222-2222-4222-8222-222222222222");
        CurrentAuthorization Permission(params string[] grants)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { sub = "alice", authorization = new { schemaVersion = 1, grants } }));
            return CurrentAuthorization.Parse(json.RootElement, "alice");
        }
        var single = Permission("telephony.panel:" + a);
        check(single.PanelAllowed && single.SingleContext == a && single.SelectContext(null) == a, "One context auto-selects");
        check(!single.CanRead(b) && !single.CanMonitor(a), "View grant grants neither foreign context nor supervision");
        try { single.SelectContext(b); throw new Exception("Foreign URL context accepted"); }
        catch (UnauthorizedAccessException) { check(true, "Foreign URL is denied"); }
        var multi = Permission("telephony.panel:" + a, "telephony.panel:" + b, "telephony.monitor:" + b);
        check(multi.SingleContext is null && multi.SelectContext(null) is null && multi.CanRead(a) && multi.CanRead(b), "Multiple contexts support an aggregate view");
        check(!multi.CanMonitor(a) && multi.CanMonitor(b), "Monitor grants stay independent by context");
        var global = Permission("telephony.panel:" + Guid.Empty, "telephony.monitor:" + a);
        check(global.GlobalPanel && global.CanRead(b) && !global.CanMonitor(b), "Global view does not grant global supervision");
        check(!Permission("telephony.panel.monitor:" + Guid.Empty).PanelAllowed, "Dotted names do not imply hierarchy");
        check(Permission("example.read:workspace/alpha", "telephony.panel:" + a).CanRead(a), "Opaque grants belonging to other applications are ignored");
        check(!Permission().PanelAllowed && !single.CanRead(Guid.Empty), "An empty URL/context is never a wildcard grant");
        check(EndpointChannelIdentity.Parse("SIP/0000006001-000000ab", null) == "0000006001", "Exact channel has a concrete endpoint identity");
        check(EndpointChannelIdentity.Parse(null, "PJSIP/0000006001-") == "0000006001", "Continuous prefix includes the channel boundary");
        foreach (var unsafeTarget in new[] { "SIP/", "SIP/0000006", "SIP/6001-*", "Local/6001@queue-", "SIP/6001/other-", "SIP/6001-\r\n", "PJSIP/wss--" })
            check(EndpointChannelIdentity.Parse(null, unsafeTarget) is null, "Unsafe or overbroad prefix is denied");
        check(EndpointChannelIdentity.Parse("", "SIP/6001-") is null, "Empty exact channel cannot change the validation target");
        var now = DateTimeOffset.UtcNow;
        ObservedResource Row(string id, string account) => new(id, "google", "peer", "PJSIP/6007", "available", "", "", account, now, now, new Dictionary<string, string>());
        var view = AuthorizedObservation.ForCustomer([new() { Label = "Own extension", Kind = Sufficit.Telephony.EventsPanel.EventsPanelCardKind.PEER, Channels = ["PJSIP/6007"] }], [Row("a", a.ToString()), Row("b", b.ToString()), Row("missing", ""), Row("empty", Guid.Empty.ToString())], single, a);
        check(view.Rows.Length == 1 && view.Rows[0].Id == "a", "Customer projection drops foreign and unknown ownership before rendering");
        var own = new Sufficit.Telephony.EventsPanel.EventsPanelCardInfo { Label = "Own", Kind = Sufficit.Telephony.EventsPanel.EventsPanelCardKind.PEER, Channels = ["^PJSIP/6007"] };
        var peers = AuthorizedObservation.ForCustomer([own], [Row("own", ""), Row("zero", Guid.Empty.ToString()), Row("foreign", b.ToString()),
            Row("other", "") with { Key = "PJSIP/6008" }, Row("longer", "") with { Key = "PJSIP/60070" }, Row("call", "") with { Kind = "call", Key = "PJSIP/6007-000000ab" }], single, a);
        check(peers.Rows.Select(r => r.Id).Order().SequenceEqual(["own", "zero"]),
            "Endpoint status without AccountCode is shown only for the context's exact catalogued extension");
        check(peers.ByCard.TryGetValue(own, out var ownRows) && ownRows.Count == 2, "Attributed endpoint status reaches the extension card");
        foreach (var unsafeCard in new[] { "PJSIP/6007", "^PJSIP/60.*", "^PJSIP/6007|^PJSIP/6008", "^Local/6007", "^PJSIP/" })
            check(AuthorizedObservation.ForCustomer([new Sufficit.Telephony.EventsPanel.EventsPanelCardInfo { Label = "Own", Kind = Sufficit.Telephony.EventsPanel.EventsPanelCardKind.PEER, Channels = [unsafeCard] }], [Row("own", "")], single, a).Rows.Length == 0,
                "Unanchored, wildcard, alternation or non-endpoint card never attributes unowned status: " + unsafeCard);
        check(AuthorizedObservation.ForCustomer([new Sufficit.Telephony.EventsPanel.EventsPanelCardInfo { Label = "Trunk", Kind = Sufficit.Telephony.EventsPanel.EventsPanelCardKind.TRUNK, Channels = ["^PJSIP/6007"] }], [Row("own", "")], single, a).Rows.Length == 0,
            "Trunk cards never attribute unowned status (trunks may be shared)");
        check(AuthorizedObservation.ForCustomer([own], [Row("own", "")], multi, null).Rows.Length == 0,
            "Without a selected context, unowned status stays hidden");
        // Parity: for the same context, catalogue and stream, a customer sees the same extension
        // status a manager sees (only status of its own catalogued extensions, never calls of others).
        var catalogue = new[] { own, new Sufficit.Telephony.EventsPanel.EventsPanelCardInfo { Label = "Second", Kind = Sufficit.Telephony.EventsPanel.EventsPanelCardKind.PEER, Channels = ["^SIP/0000286651"] } };
        var stream = new[] { Row("p1", ""), Row("p2", "") with { Key = "SIP/0000286651" }, Row("x", "") with { Key = "SIP/0000999999" } };
        var managerTiles = OperatorTile.Create(catalogue, AuthorizedObservation.Create(catalogue, stream, a).ByCard);
        var customerTiles = OperatorTile.Create(catalogue, AuthorizedObservation.ForCustomer(catalogue, stream, single, a).ByCard);
        check(managerTiles.Select(t => (t.Title, t.Node, t.Status.Tone)).SequenceEqual(customerTiles.Select(t => (t.Title, t.Node, t.Status.Tone))),
            "Customer and manager see the same extension status for the same context");
        var all = AuthorizedObservation.ForCustomer([], [Row("a", a.ToString()), Row("b", b.ToString())], multi, null);
        check(all.Rows.Length == 2, "Aggregate projection contains only authorized contexts");
        var tiles = OperatorTile.Create(all.Cards, all.ByCard, true);
        check(tiles.Length == 2 && tiles.Select(tile => tile.Id).Distinct().Count() == 2
            && tiles.All(tile => tile.Rows.Length == 1), "Same endpoint name in two contexts remains isolated");
        var clock = new FakeClock();
        var http = new FakeHttp { Body = JsonSerializer.Serialize(new { sub = "alice", authorization = new { schemaVersion = 1, grants = new[] { "telephony.panel:" + a } } }) };
        using var client = new CurrentAuthorizationClient(http, clock);
        check((await client.ReadAsync("alice", "synthetic", default)).CanRead(a), "Current permissions are fetched");
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.ReadAsync("alice", "synthetic", default)));
        check(http.Requests == 1, "Concurrent permission reads share bounded cache");
        clock.Now += TimeSpan.FromSeconds(61);
        http.Status = HttpStatusCode.ServiceUnavailable;
        try { await client.ReadAsync("alice", "synthetic", default); throw new Exception("Expired allow survived failure"); }
        catch (HttpRequestException) { check(true, "No stale allow on Identity failure"); }
        http.Status = HttpStatusCode.OK;
        http.Body = "{\"sub\":\"alice\",\"authorization\":{\"schemaVersion\":1,\"grants\":[]}}";
        check(!(await client.ReadAsync("alice", "synthetic", default)).PanelAllowed, "Revocation applies without replacing token");
        try { await client.ReadAsync("bob", "synthetic", default); throw new Exception("Subject mismatch accepted"); }
        catch (UnauthorizedAccessException) { check(true, "Permission cache and response are bound to subject"); }
    }
}
