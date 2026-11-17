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
        var card = new EventsPanelCardInfo { Label = "6007", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/6007"] };
        var byCard = new Dictionary<EventsPanelCardInfo, List<ObservedResource>> { [card] = [peer, peer with { Node = "google", Id = "other" }] };
        var tiles = OperatorTile.Create([card], byCard);
        check(tiles.Length == 2 && tiles.All(t => t.Rows.Length == 1), "Tile status isolated per server");
        check(tiles.All(t => t.Kind == EventsPanelCardKind.PEER), "No trunk classification inferred from channel name");
        check(OperatorTile.Create([card], new Dictionary<EventsPanelCardInfo, List<ObservedResource>>()).Single().Rows.Length == 0, "Configured resource without events remains visible and unknown");
    }
}
