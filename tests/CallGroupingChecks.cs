using System.Text.Json;
using Sufficit.Telephony.Panel.Operations;

internal static class CallGroupingChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        ObservedResource Channel(string id, string? linked, string node = "eveo", string state = "Up") =>
            new(id, node, "call", id, state, "same caller", "6007", null, now, now,
                linked is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["linkedid"] = linked });
        var sip = Channel("SIP/test-01", "100.1");
        var local1 = Channel("Local/test;1", "100.1", state: "Down");
        var local2 = Channel("Local/test;2", "100.1", state: "Ring");
        var groups = ObservedCallGroup.Create([sip, local1, local2]);
        check(groups.Length == 1 && groups[0].Channels.Length == 3, "SIP and both Local legs count as one linked call");
        check(groups[0].Channels.Any(c => c.State == "Down"), "Down leg does not terminate its call group");
        check(ObservedCallGroup.Create([sip, Channel("other-node", "100.1", "google")]).Length == 2, "Linkedid is scoped to server");
        check(ObservedCallGroup.Create([sip, Channel("other-id", "100.2")]).Length == 2, "Same caller/destination cannot merge different linked IDs");
        var unknown = ObservedCallGroup.Create([Channel("missing", null), Channel("blank", " ")]);
        check(unknown.Length == 2 && unknown.All(g => !g.Correlated), "Missing Linkedid never fabricates correlated calls");
        check(ObservedCallGroup.Filter(groups, c => c.Key == sip.Key).Single().Channels.Length == 3, "Text/state matches retain authorized sibling legs");
        check(ObservedCallGroup.Create([sip, sip with { Id = "peer", Kind = "peer" }]).Single().Channels.Length == 1, "Peer rows never counted as channels");
        check(ObservedCallGroup.Create([sip]).Single().Channels.Length == 1, "Grouping cannot reintroduce unauthorized rows");
        var projection = new LiveProjection();
        foreach (var c in new[] { sip, local1, local2 })
            projection.Apply(c.Node, "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel = c.Key, linkedid = "100.1" }), now);
        projection.Apply("eveo", "HangupEvent", JsonSerializer.SerializeToElement(new { channel = local1.Key }), now);
        check(ObservedCallGroup.Create(projection.Snapshot(now)).Single().Channels.Length == 2, "Hangup removes one leg, not the entire group");
        foreach (var c in new[] { sip, local2 }) projection.Apply("eveo", "HangupEvent", JsonSerializer.SerializeToElement(new { channel = c.Key }), now);
        check(ObservedCallGroup.Create(projection.Snapshot(now)).Length == 0, "Group disappears after final observed leg ends");
    }
}
