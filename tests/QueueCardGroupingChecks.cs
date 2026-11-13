using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class QueueCardGroupingChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var owner = Guid.NewGuid(); var queue = Guid.NewGuid();
        OperatorTile Tile(string node, int pending, int connected, string state = "current")
        {
            var key = $"queue:{owner:N}:{queue:N}";
            var call = new ObservedResource(node + "|call", node, "call", "SIP/6001-abcdef01", "Up", node, "6001", owner.ToString("N"), now, now, new Dictionary<string,string>());
            var metadata = new ObservedResource(node + "|queue", node, "queue", key, state, null, null, owner.ToString("N"), now, now,
                new Dictionary<string,string> { ["source"] = "queue", ["pending"] = pending.ToString(), ["connected"] = connected.ToString(), ["correlation"] = "visits" });
            return new(node, "Atendimento", node, EventsPanelCardKind.QUEUE, [metadata]) {
                Keys = [key], Flows = [new(node + "|flow", node, "6001", "waiting", true, [call])]
            };
        }
        var a = Tile("eveo", 2, 1); var b = Tile("apoint", 3, 2);
        var merged = QueueCardGrouping.Merge([a, b]);
        check(merged.Length == 1, "Same company/GUID must produce one queue card across servers");
        var card = merged.Single();
        check(BoardTotals.Waiting(merged) == 5 && card.Queue!.Details["connected"] == "3", "One card sums pending and connected calls across current servers");
        check(card.Flows.Length == 2 && card.DetailChannels.Select(r=>r.Node).Distinct().Count() == 2, "Both servers' calls retain their exact supervision channels");
        check(card.Rows.Length == 2 && QueueResourceIdentity.From(card) == new QueueResourceIdentity(owner, queue), "Original rows and proven management identity survive grouping");
        check(QueueCardGrouping.Merge([card]).Single().Queue!.Details["pending"] == "5", "Reapplying grouping never doubles the synthetic summary");
        var duplicate = a with { Id = "alias", Title = "Outro título" };
        check(BoardTotals.Waiting(QueueCardGrouping.Merge([a, duplicate, b])) == 5, "Duplicate portal cards on the same node do not double counts");
        var stale = Tile("google", 100, 100, "stale");
        var partial = QueueCardGrouping.Merge([a, stale]).Single();
        check(BoardTotals.Waiting([partial]) == 2 && partial.QueueStateIncomplete, "Stale counts never become live totals and remain marked partial");
        var historic = b with { Rows = [], Flows = [], LastSeen = now.AddDays(-1) };
        check(QueueCardGrouping.Merge([a, historic]).Length == 1, "Historic replicas fold into the identified queue");
        check(QueueCardGrouping.Merge([historic]).Single().Remembered, "Historical-only queue stays historical");
        check(QueueCardGrouping.Merge([a]).Single().Id == card.Id && QueueCardGrouping.Merge([b,a]).Single().Id == card.Id, "Selection identity is stable across node changes and order");
        check(QueueCardGrouping.Merge([a, b with { Keys = [$"queue:{Guid.NewGuid():N}:{queue:N}"], Rows = [] }]).Length == 2, "Equal GUIDs in different companies never merge");
        check(QueueCardGrouping.Merge([a with { Keys = ["0001"], Rows = [] }, b with { Keys = ["0001"], Rows = [] }]).Length == 2, "Unproven numeric identities never combine customers");
        check(QueueCardGrouping.Merge([a, b with { Rows = b.Rows.Select(r=>r with { Account = Guid.NewGuid().ToString("N") }).ToArray() }]).Length == 2, "Conflicting ownership cannot establish a common queue");
        var rules = new[] { "eveo", "apoint", "unobserved" }.Select(node => new SupervisionPrefixRule {
            Id = "rule-" + node, ContextId = owner, Node = node, CardKey = a.Keys[0], Prefix = "SIP/6001-"
        }).ToArray();
        var policy = new SupervisionPrefixPolicy(rules);
        check(policy.For(card, owner).Length == 2, "Grouped queue keeps protected prefix choices on each observed original node");
        check(policy.For(card, Guid.NewGuid()).Length == 0, "Grouped prefix choices never cross the selected company");
        check(policy.For(card with { Rows = [] }, owner).Length == 0, "A synthetic summary alone cannot authorize a node for prefix supervision");
        var peer = a with { Kind = EventsPanelCardKind.PEER };
        check(QueueCardGrouping.Merge([peer, b]).Contains(peer), "Extension cards are unaffected");
    }
}
