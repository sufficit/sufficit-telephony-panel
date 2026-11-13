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
        var answered = legacy with { Rows = [Row("a1", "waiting", "answered")] };
        check(BoardTotals.Waiting([answered]) == 0, "Answered queue calls are not waiting");
        var truncated = native with { Rows = [..native.Rows, Row("v1", "waiting", "waiting"), Row("v2", "waiting", "answered")] };
        check(BoardTotals.Waiting([truncated]) == 3, "Authoritative pending wins over truncated or connected visit details");
        check(BoardTotals.Waiting([legacy with { Rows = [Row("a2", "waiting", "connected")] }]) == 0, "Connected visits are not waiting");
        var owner = Guid.NewGuid(); var queueId = Guid.NewGuid();
        var observed = native with { Keys = [$"queue:{owner:N}:{queueId:N}"] };
        var replica = observed with { Id = "replica", Node = "apoint" };
        var history = observed with { Id = "history", Rows = [], LastSeen = now.AddDays(-1) };
        check(BoardTotals.ObservedQueues([observed, replica, history, stale]) == 1,
            "Same native queue on two nodes counts once; remembered and stale cards do not inflate the observed queue count");
        check(BoardTotals.ObservedQueues([observed, replica with { Keys = [$"queue:{Guid.NewGuid():N}:{queueId:N}"] }]) == 2,
            "Queue identities from different companies are never combined");
        check(!BoardTotals.CurrentQueue(history) && !BoardTotals.CurrentQueue(stale) && BoardTotals.CurrentQueue(observed),
            "Current queue evidence distinguishes archive and unavailable source state");
        check(BoardTotals.ActiveCalls([native, busy]) == 3, "A busy card without channels counts as one call, cards with channels count their channels");
    }
}
