using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class QueueCardOrderChecks
{
    public static void Run(Action<bool,string> check)
    {
        var now = DateTimeOffset.UtcNow;
        OperatorTile Tile(string name, int waiting, int connected, string state = "current") =>
            new(name, name, "eveo", EventsPanelCardKind.QUEUE, [new(name, "eveo", "queue", name, state, null, null, null, now, now,
                new Dictionary<string,string> { ["source"] = "queue", ["pending"] = waiting.ToString(), ["connected"] = connected.ToString() })]);
        var small = Tile("A pequena", 1, 0); var busy = Tile("Z utilizada", 2, 4);
        var waiting = Tile("B espera", 6, 0); var stale = Tile("Antiga", 500, 500, "stale");
        var history = Tile("Histórica", 0, 0) with { Rows = [], LastSeen = now };
        var idle = Tile("Ociosa", 0, 0);
        check(QueueCardOrder.Sort([small,busy], true)[0] == busy, "Manager overview ranks six active visits above one regardless of name");
        check(QueueCardOrder.Sort([busy,waiting], true)[0] == waiting, "Equal utilization prioritizes waiting calls");
        var ordered = QueueCardOrder.Sort([history,stale,idle,small,busy,waiting], true);
        check(ordered.Take(3).SequenceEqual(new[] {waiting,busy,small}), "Active queue usage precedes empty, stale and historical cards");
        check(Array.IndexOf(ordered,idle) < Array.IndexOf(ordered,stale), "Stale large counts cannot outrank current empty state");
        check(QueueCardOrder.Sort([small,busy], false)[0] == small, "Company view preserves its existing ordering");
    }
}
