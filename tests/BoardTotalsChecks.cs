using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class BoardTotalsChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        ObservedResource Row(string id, string kind, string state = "Up", Dictionary<string, string>? details = null)
            => new(id, "eveo", kind, id, state, "6001", "6002", Guid.Empty.ToString("N"), now, now, details ?? new Dictionary<string, string>());
        var busy = new OperatorTile("p1", "6001", "eveo", EventsPanelCardKind.PEER, [Row("PJSIP/6001-01", "call"), Row("PJSIP/6001-02", "call")]);
        var ringing = new OperatorTile("p2", "6002", "eveo", EventsPanelCardKind.PEER, [Row("PJSIP/6002-01", "call", "Ringing")]);
        var silent = new OperatorTile("p3", "6003", "eveo", EventsPanelCardKind.PEER, []);
        var peers = new[] { busy, ringing, silent };
        check(BoardTotals.ActiveCalls(peers) == 3, "Extension calls count each channel once");
        check(BoardTotals.Reachable(peers) == 2, "Busy and ringing extensions count as reachable, cards without state do not");

        var trunk = new OperatorTile("t1", "operadora", "eveo", EventsPanelCardKind.TRUNK, [Row("PJSIP/trunk-01", "call"), Row("PJSIP/trunk-01", "call")]);
        check(BoardTotals.ActiveCalls([trunk]) == 1, "A channel repeated on the card counts once");

        var legacy = new OperatorTile("q1", "Suporte", "eveo", EventsPanelCardKind.QUEUE, [Row("w1", "waiting"), Row("w2", "waiting")]);
        var native = new OperatorTile("q2", "Vendas", "eveo", EventsPanelCardKind.QUEUE,
            [Row("queue", "queue", "current", new Dictionary<string, string> { ["source"] = "queue", ["pending"] = "3" })]);
        var stale = new OperatorTile("q3", "Antiga", "eveo", EventsPanelCardKind.QUEUE,
            [Row("queue", "queue", "stale", new Dictionary<string, string> { ["source"] = "queue", ["pending"] = "9" })]);
        check(BoardTotals.Waiting([legacy, native, stale]) == 5, "Waiting adds observed visits and current native pending, never a stale count");
        check(BoardTotals.ActiveCalls([native, busy]) == 3, "A busy card without channels counts as one call, cards with channels count their channels");
    }
}
