using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Operations;

internal static class OperatorBoardChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var peer = new ObservedResource("peer", "eveo", "peer", "PJSIP/6007", "unknown", "", "", null, now, now, new Dictionary<string, string>());
        check(OperatorTile.Describe([]).Tone == "unknown", "Missing observations are not green availability");
        check(OperatorTile.Describe([peer with { Peer = new() { Registration = "registered" } }]).Label == "Registrado no evento", "Registration not relabeled free");
        check(OperatorTile.Describe([peer with { Peer = new() { Reachability = "offline" } }]).Tone == "offline", "Negative reachability retains distinct state");
        check(OperatorTile.Describe([peer with { Kind = "call", State = "Down" }]).Tone == "busy", "Observed Down channel not treated as a free extension");
        check(OperatorTile.Describe([peer with { Kind = "call", State = "Ringing" }]).Tone == "ringing", "Ringing channel receives text and distinct tone");
        var card = new EventsPanelCardInfo { Label = "6007", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/6007"] };
        var byCard = new Dictionary<EventsPanelCardInfo, List<ObservedResource>> { [card] = [peer, peer with { Node = "google", Id = "other" }] };
        var tiles = OperatorTile.Create([card], byCard);
        check(tiles.Length == 2 && tiles.All(t => t.Rows.Length == 1), "Tile status isolated per server");
        check(tiles.All(t => t.Kind == EventsPanelCardKind.PEER), "No trunk classification inferred from channel name");
        check(OperatorTile.Create([card], new Dictionary<EventsPanelCardInfo, List<ObservedResource>>()).Single().Rows.Length == 0, "Configured resource without events remains visible and unknown");
    }
}
