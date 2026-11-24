using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

internal static class OperatorBoardChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var peer = new ObservedResource("peer", "eveo", "peer", "PJSIP/6007", "unknown", "", "", null, now, now, new Dictionary<string, string>());
        check(OperatorTile.Describe([]).Tone == "unknown", "Missing observations are not green availability");
        check(OperationsApi.FallbackLabel(["^SIP/0000286651"]) == "SIP/0000286651", "Blank portal description falls back to the endpoint identifier");
        check(OperationsApi.FallbackLabel([]) == "—", "Card without channels still gets a visible title");
        check(OperatorTile.Describe([peer with { Peer = new() { Registration = "registered" } }]).Label == "Registrado no evento", "Registration not relabeled free");
        check(OperatorTile.Describe([peer with { Peer = new() { Reachability = "offline" } }]).Tone == "offline", "Negative reachability retains distinct state");
        check(OperatorTile.Describe([peer with { Kind = "call", State = "Down" }]).Tone == "busy", "Observed Down channel not treated as a free extension");
        check(OperatorTile.Describe([peer with { Kind = "call", State = "Ringing" }]).Tone == "ringing", "Ringing channel receives text and distinct tone");
        foreach (var state in new[] { "Ring", "4", "Dialing", "3", "Dialing Offhook", "DialingOffhook", "8", "Pre-ring", "PreRing", "9" })
            check(OperatorTile.Describe([peer with { Kind = "call", State = state }]).Tone == "dialing", $"{state} is a calling attempt, not confirmed ringing");
        foreach (var state in new[] { "Ringing", "5", "RINGING" })
            check(OperatorTile.Describe([peer with { Kind = "call", State = state }]).Tone == "ringing", $"{state} confirms ringing");
        var calling = peer with { Kind = "call", State = "Ring" };
        var ringing = calling with { State = "Ringing" };
        check(OperatorTile.Describe([calling, ringing]).Tone == "ringing", "Confirmed ringing wins over another dialing channel");
        check(OperatorTile.Describe([ringing with { State = "Up" }]).Tone == "busy", "Answer removes ringing motion");
        check(OperatorTile.Describe([peer with { Peer = new() { Registration = "registered" } }]).Tone == "online", "Removing the channel restores registration state");
        check(OperatorTile.Describe([peer with { Kind = "call", State = "unknown" }]).Tone == "busy", "Unknown channel state does not invent a calling attempt");
        var dialingTile = new OperatorTile("dialing", "6007", "eveo", EventsPanelCardKind.PEER, [calling]);
        check(BoardTotals.Reachable([dialingTile]) == 1, "Dialing retains the existing reachable total");
        check(BoardTotals.ActiveCalls([dialingTile]) == 1, "Dialing retains the active call total");
        // Ring on the caller can persist while another authorized leg is already Ringing.
        var account = Guid.NewGuid().ToString("N");
        ObservedResource Leg(string id, string state, string linked = "call-1", string node = "eveo", string? tenant = null) =>
            new(id, node, "call", id, state, "caller", "6007", tenant ?? account, now, now,
                new Dictionary<string, string> { ["linkedid"] = linked });
        var origin = Leg("PJSIP/origin-000000aa", "Ring");
        var destination = Leg("SIP/6007-000000bb", "Ringing");
        var originTile = new OperatorTile("origin", "Origin", "eveo", EventsPanelCardKind.PEER, [origin]);
        OperatorTile OriginWith(params ObservedResource[] rows) => BoardCallFlow.Attach([originTile], rows).Single();
        check(OriginWith(origin, destination).Status == ("ringing", "Destination ringing"), "Caller Ring reflects confirmed ringing on the authorized destination");
        check(OperatorTile.Describe([origin]).Tone == "dialing", "Ringback presentation does not rewrite the raw caller channel state");
        check(OriginWith(origin).Status.Tone == "dialing", "No destination evidence retains dialing");
        check(OriginWith(origin, destination with { State = "Down" }).Status.Tone == "dialing", "Down destination cannot confirm ringing");
        check(OriginWith(origin, destination with { State = "Up" }).Status.Tone != "ringing", "Answer removes destination ringing");
        check(OriginWith(origin, Leg("SIP/6007-000000bb", "Ringing", "other-call")).Status.Tone == "dialing", "Matching dialed numbers cannot merge independent calls");
        check(OriginWith(origin, Leg("SIP/6007-000000bb", "Ringing", node: "google")).Status.Tone == "dialing", "Linked IDs cannot cross PBX nodes");
        check(OriginWith(origin, Leg("SIP/6007-000000bb", "Ringing", tenant: Guid.NewGuid().ToString("N"))).Status.Tone == "dialing", "Conflicting accounts cannot share ringing evidence");
        check(OriginWith(origin, destination with { Key = "Local/6007@queue;2" }).Status.Tone == "dialing", "Technical Local ringing does not confirm that an endpoint is ringing");
        var answeredOrigin = originTile with { Rows = [origin with { State = "Up" }] };
        check(BoardCallFlow.Attach([answeredOrigin], [answeredOrigin.Rows[0], destination]).Single().Status.Tone == "busy", "An answered origin is not reanimated by another ringing leg");
        var trunkOrigin = originTile with { Kind = EventsPanelCardKind.TRUNK };
        check(BoardCallFlow.Attach([trunkOrigin], [origin, destination]).Single().Status.Tone == "dialing", "Ringback presentation is restricted to extensions");
        var card = new EventsPanelCardInfo { Label = "6007", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/6007"] };
        var byCard = new Dictionary<EventsPanelCardInfo, List<ObservedResource>> { [card] = [peer, peer with { Node = "google", Id = "other" }] };
        var tiles = OperatorTile.Create([card], byCard);
        check(tiles.Length == 1 && tiles[0].Rows.Length == 2 && tiles[0].Nodes.Length == 2 && tiles[0].Device == "pjsip",
            "One tile per device type: the same device seen on two servers is one tile, servers are detail");
        var webrtcPeer = peer with { Id = "webrtc", Key = "PJSIP/wss-6007" };
        var withWebRtc = OperatorTile.Create([card], new Dictionary<EventsPanelCardInfo, List<ObservedResource>> { [card] = [peer, webrtcPeer] });
        check(withWebRtc.Length == 2 && withWebRtc.Select(t => t.Device).Order().SequenceEqual(["pjsip", "webrtc"]) && withWebRtc.All(t => t.Rows.Length == 1),
            "Each PJSIP device type of an extension keeps its own tile");
        check(tiles.All(t => t.Kind == EventsPanelCardKind.PEER), "No trunk classification inferred from channel name");
        check(OperatorTile.Create([card], new Dictionary<EventsPanelCardInfo, List<ObservedResource>>()).Single().Rows.Length == 0, "Configured resource without events remains visible and unknown");
    }
}
