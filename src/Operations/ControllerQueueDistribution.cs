using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Queue distribution for queues run by the Queue controller (sufficit-ari). Those queues do not use
/// app_queue, so the collector never sees AgentCalled/AgentConnect for them; instead the controller
/// already persists per-offer events (offered, ringing, connected, offer_finished). This class reads the
/// last 24 hours through the existing diagnostics API and shapes them like the collector's reply.
/// Finished visits never change, so their events are cached; the per-source result is rebuilt at most
/// every 4 seconds. Callers must authorize the context before calling <see cref="Read"/>.
/// </summary>
public sealed class ControllerQueueDistribution(QueueDiagnosticsReader reader, TimeProvider clock)
{
    private const int PageLimit = 100, MaxPages = 20, MaxEventPages = 5, VisitCacheLimit = 5000;
    private readonly Dictionary<Guid, QueueDiagnosticEvent[]> finished = [];
    private readonly Dictionary<(Guid, Guid), (DateTimeOffset At, DistributionReply Reply)> results = [];
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<DistributionReply> Read(Guid context, Guid node, string nodeName, IReadOnlyDictionary<Guid, string> titles, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var now = clock.GetUtcNow();
            if (results.TryGetValue((context, node), out var cached) && now - cached.At < TimeSpan.FromSeconds(4)) return cached.Reply;
            var until = now; var from = now.AddHours(-QueueDistribution.Hours).AddMinutes(1);
            var calls = new List<QueueDiagnosticCall>();
            string? cursor = null;
            for (var page = 0; page < MaxPages; page++)
            {
                var path = $"calls?from={Uri.EscapeDataString(from.ToString("O"))}&until={Uri.EscapeDataString(until.ToString("O"))}&limit={PageLimit}";
                if (cursor is not null) path += "&cursor=" + Uri.EscapeDataString(cursor);
                var result = await reader.Read<QueueDiagnosticCalls>(context, node, path, ct);
                calls.AddRange(result.Items);
                if ((cursor = result.NextCursor) is null) break;
            }
            var events = new Dictionary<Guid, QueueDiagnosticEvent[]>();
            foreach (var call in calls)
            {
                if (finished.TryGetValue(call.VisitId, out var known)) { events[call.VisitId] = known; continue; }
                var items = new List<QueueDiagnosticEvent>();
                long after = 0;
                for (var page = 0; page < MaxEventPages; page++)
                {
                    var result = await reader.Read<QueueDiagnosticEvents>(context, node, $"calls/{call.VisitId:N}/events?after={after}&limit=100", ct);
                    items.AddRange(result.Items);
                    if (result.NextCursor is not { } next) break;
                    after = next;
                }
                events[call.VisitId] = [.. items];
                if (call.EndedAt is not null)
                {
                    if (finished.Count >= VisitCacheLimit) finished.Clear();
                    finished[call.VisitId] = [.. items];
                }
            }
            var reply = Build(context, nodeName, calls, events, titles, now);
            results[(context, node)] = (now, reply);
            return reply;
        }
        finally { gate.Release(); }
    }

    private sealed class Attempt
    {
        public required string Extension;
        public DateTimeOffset OfferedAt;
        public DateTimeOffset? RangAt;
        public string Outcome = "ringing";
        public long RingSeconds, TalkSeconds;
    }

    /// <summary>Pure shaping of controller calls and per-offer events into hourly buckets (oldest first, last = current hour).</summary>
    public static DistributionReply Build(Guid context, string node, IEnumerable<QueueDiagnosticCall> calls,
        IReadOnlyDictionary<Guid, QueueDiagnosticEvent[]> events, IReadOnlyDictionary<Guid, string> titles, DateTimeOffset now)
    {
        var account = context.ToString("N");
        var currentHour = now.ToUnixTimeSeconds() / 3600;
        int? Slot(DateTimeOffset at) { var offset = currentHour - at.ToUnixTimeSeconds() / 3600; return offset is >= 0 and < QueueDistribution.Hours ? (int)(QueueDistribution.Hours - 1 - offset) : null; }
        static long Seconds(DateTimeOffset? end, DateTimeOffset? start) => end is { } e && start is { } s && e > s ? (long)(e - s).TotalSeconds : 0;
        var queueStats = new Dictionary<Guid, QueueStat>();
        var memberStats = new Dictionary<(Guid, string), MemberStat>();
        QueueStat QueueFor(Guid id)
        {
            if (queueStats.TryGetValue(id, out var stat)) return stat;
            stat = new QueueStat { Node = node, Queue = id.ToString("N"), Account = account, Title = titles.GetValueOrDefault(id, "") };
            foreach (var list in new[] { stat.Entered, stat.Answered, stat.Abandoned, stat.WaitSeconds }) list.AddRange(new long[QueueDistribution.Hours]);
            return queueStats[id] = stat;
        }
        MemberStat MemberFor(Guid queue, string extension)
        {
            if (memberStats.TryGetValue((queue, extension), out var stat)) return stat;
            stat = new MemberStat { Node = node, Queue = queue.ToString("N"), Account = account, Interface = extension };
            foreach (var list in new[] { stat.Offered, stat.Answered, stat.Missed, stat.Unavailable, stat.RingSeconds, stat.TalkSeconds }) list.AddRange(new long[QueueDistribution.Hours]);
            return memberStats[(queue, extension)] = stat;
        }
        var reply = new DistributionReply { NowMs = now.ToUnixTimeMilliseconds(), StartedAtMs = now.AddHours(-QueueDistribution.Hours).ToUnixTimeMilliseconds() };
        foreach (var call in calls.OrderByDescending(c => c.EnteredAt))
        {
            var queue = QueueFor(call.QueueId);
            var outcome = call.AnsweredAt is not null ? "answered" : call.Status == "abandoned" ? "abandoned" : call.EndedAt is not null ? "left" : "waiting";
            var wait = Seconds(call.AnsweredAt ?? call.EndedAt, call.EnteredAt);
            if (Slot(call.EnteredAt) is { } entered)
            {
                queue.Entered[entered]++;
                if (outcome == "answered") { queue.Answered[entered]++; queue.WaitSeconds[entered] += wait; }
                if (outcome == "abandoned") { queue.Abandoned[entered]++; queue.WaitSeconds[entered] += wait; }
            }
            var attempts = new List<Attempt>();
            foreach (var item in events.GetValueOrDefault(call.VisitId, []).OrderBy(e => e.Id))
            {
                if (string.IsNullOrWhiteSpace(item.Extension)) continue;
                var attempt = attempts.LastOrDefault(a => a.Extension == item.Extension);
                switch (item.Kind)
                {
                    case "offered":
                        attempts.Add(new Attempt { Extension = item.Extension, OfferedAt = item.OccurredAt });
                        break;
                    case "ringing" when attempt is not null:
                        attempt.RangAt ??= item.OccurredAt;
                        break;
                    case "connected" when attempt is not null:
                        attempt.Outcome = "answered";
                        attempt.RingSeconds = Seconds(item.OccurredAt, attempt.RangAt);
                        attempt.TalkSeconds = Seconds(call.EndedAt, call.AnsweredAt ?? item.OccurredAt);
                        break;
                    case "offer_finished" when attempt is { Outcome: "ringing" }:
                        // channel_ended: the member's leg hung up before answering. Within 2 s of dialing it failed
                        // (unregistered/offline, like the collector's zero ring time); later it rang and was declined.
                        if (item.Reason == "channel_ended" && attempt.RangAt is { } rang)
                        {
                            var ring = Seconds(item.OccurredAt, rang);
                            attempt.Outcome = ring < 2 ? "unavailable" : "no-answer";
                            if (ring >= 2) attempt.RingSeconds = ring;
                            break;
                        }
                        attempt.Outcome = item.Reason is "no_answer_or_busy" or "ring_timeout" ? "no-answer"
                            : attempt.RangAt is null || item.Reason is "agent_media_failed" or "offer_expired_before_dial" ? "unavailable"
                            : outcome == "answered" ? "other-answered" : "caller-hangup";
                        if (attempt.Outcome == "no-answer") attempt.RingSeconds = Seconds(item.OccurredAt, attempt.RangAt);
                        break;
                }
            }
            var trace = new CallTrace { Node = node, Queue = queue.Queue, Account = account, Uniqueid = call.VisitId.ToString("N"),
                EnteredMs = call.EnteredAt.ToUnixTimeMilliseconds(), Outcome = outcome, WaitSeconds = wait };
            foreach (var attempt in attempts.Where(a => a.Outcome == "ringing" && outcome != "waiting"))
                attempt.Outcome = outcome == "answered" ? "other-answered" : "caller-hangup";
            // Ringall retries offer every member again each cycle: fold them into one result per member per call
            // (answered > rang and not answered > only fast failures > someone else answered / caller left).
            static int Rank(string o) => o switch { "answered" => 4, "no-answer" => 3, "unavailable" => 2, _ => 1 };
            var folded = attempts.GroupBy(a => a.Extension).Select(g =>
            {
                var best = g.OrderByDescending(a => Rank(a.Outcome)).ThenByDescending(a => a.RingSeconds).First();
                return (Attempt: new Attempt { Extension = g.Key, OfferedAt = g.Min(a => a.OfferedAt), Outcome = best.Outcome,
                    RingSeconds = g.Max(a => a.RingSeconds), TalkSeconds = g.Max(a => a.TalkSeconds) }, Cycles: g.Count());
            }).ToList();
            foreach (var (attempt, cycles) in folded)
            {
                trace.Attempts.Add(new CallAttempt { Interface = attempt.Extension, CalledMs = attempt.OfferedAt.ToUnixTimeMilliseconds(),
                    Outcome = attempt.Outcome, RingSeconds = attempt.RingSeconds, TalkSeconds = attempt.TalkSeconds, Cycles = cycles });
                if (Slot(attempt.OfferedAt) is not { } slot) continue;
                var member = MemberFor(call.QueueId, attempt.Extension);
                member.Offered[slot]++;
                switch (attempt.Outcome)
                {
                    case "answered":
                        member.Answered[slot]++; member.RingSeconds[slot] += attempt.RingSeconds; member.TalkSeconds[slot] += attempt.TalkSeconds;
                        member.LastAnsweredMs = Math.Max(member.LastAnsweredMs, (call.AnsweredAt ?? attempt.OfferedAt).ToUnixTimeMilliseconds());
                        break;
                    case "no-answer": member.Missed[slot]++; member.RingSeconds[slot] += attempt.RingSeconds; break;
                    case "unavailable": member.Unavailable[slot]++; break;
                }
            }
            if (reply.Calls.Count < 500) reply.Calls.Add(trace);
        }
        reply.Queues.AddRange(queueStats.Values);
        reply.Members.AddRange(memberStats.Values);
        return reply;
    }
}
