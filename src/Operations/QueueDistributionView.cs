using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record DistributionAttempt(string Member, string Name, DateTimeOffset CalledAt, string Outcome, long RingSeconds, long TalkSeconds);
public sealed record DistributionCall(string Uniqueid, string Caller, DateTimeOffset EnteredAt, string Outcome, long WaitSeconds, DistributionAttempt[] Attempts);
public sealed record DistributionMember(string Member, string Name, long Offered, long Answered, long Missed, long Unavailable,
    long RingSeconds, long TalkSeconds, DateTimeOffset? LastAnswered, long[] AnsweredByHour, long[] RungByHour)
{
    /// <summary>"Local/0007366001@from-queue/n" → "0007366001"; SIP/PJSIP keep the endpoint name.</summary>
    public string Extension => QueueDistributionView.Extension(Member);
    public string Label => Name.Length > 0 && Name != Member ? Name : Extension;
    /// <summary>Rang with the call still open when it resolved: someone else answered or the caller left.</summary>
    public long Cancelled => Math.Max(0, Offered - Answered - Missed - Unavailable);
    public double? AnswerRate => Offered - Unavailable > 0 ? (double)Answered / (Offered - Unavailable) : null;
    public long? AverageRing => Answered + Missed > 0 ? RingSeconds / (Answered + Missed) : null;
    public long? AverageTalk => Answered > 0 ? TalkSeconds / Answered : null;
}
public sealed record DistributionQueue(Guid? Account, string Node, string Queue, long Entered, long Answered, long Abandoned,
    long WaitSeconds, long[] EnteredByHour, long[] AnsweredByHour, long[] AbandonedByHour, string Title = "")
{
    public string Label => Title.Length > 0 ? Title : Queue;
    public string Key => Node + "|" + Queue;
    public long Other => Math.Max(0, Entered - Answered - Abandoned);
    public double? AnswerRate => Entered > 0 ? (double)Answered / Entered : null;
    public long? AverageWait => Answered + Abandoned > 0 ? WaitSeconds / (Answered + Abandoned) : null;
}
public sealed record DistributionDetail(DistributionQueue Queue, DistributionMember[] Members, DistributionCall[] Calls);

/// <summary>
/// Presentation of the collector's in-memory 24-hour queue distribution. Hourly buckets follow the clock:
/// a window of N hours sums the current (partial) hour and the N-1 previous ones.
/// </summary>
public static class QueueDistributionView
{
    public static readonly int[] Windows = [1, 4, 24];

    public static string Extension(string member)
    {
        var name = member.Contains('/') ? member[(member.IndexOf('/') + 1)..] : member;
        var at = name.IndexOfAny(['@', '/', '-']);
        return at > 0 ? name[..at] : name;
    }

    private static long[] Last(IEnumerable<long> values, int hours)
    {
        var all = values.ToArray();
        return all.Skip(Math.Max(0, all.Length - hours)).ToArray();
    }

    private static Guid? Account(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    private static DateTimeOffset? Time(long ms) => ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;

    public static DistributionQueue Queue(QueueStat stat, int hours)
    {
        var entered = Last(stat.Entered, hours); var answered = Last(stat.Answered, hours); var abandoned = Last(stat.Abandoned, hours);
        return new(Account(stat.Account), stat.Node, stat.Queue, entered.Sum(), answered.Sum(), abandoned.Sum(),
            Last(stat.WaitSeconds, hours).Sum(), entered, answered, abandoned, stat.Title);
    }

    public static DistributionQueue[] Queues(DistributionReply reply, int hours) => reply.Queues
        .Select(q => Queue(q, hours))
        .Where(q => q.Entered > 0 || hours == 24)
        .OrderByDescending(q => q.Entered).ThenBy(q => q.Queue, StringComparer.Ordinal).ToArray();

    public static DistributionDetail? Detail(DistributionReply reply, string node, string queue, int hours)
    {
        if (reply.Queues.FirstOrDefault(q => q.Node == node && q.Queue == queue) is not { } stat) return null;
        var since = DateTimeOffset.FromUnixTimeMilliseconds(reply.NowMs).AddHours(-hours);
        var members = reply.Members.Where(m => m.Node == node && m.Queue == queue).Select(m =>
        {
            var rung = Last(m.Offered, hours);
            return new DistributionMember(m.Interface, m.Name, rung.Sum(), Last(m.Answered, hours).Sum(), Last(m.Missed, hours).Sum(),
                Last(m.Unavailable, hours).Sum(), Last(m.RingSeconds, hours).Sum(), Last(m.TalkSeconds, hours).Sum(),
                Time(m.LastAnsweredMs), Last(m.Answered, hours), rung);
        }).Where(m => m.Offered > 0 || m.Answered > 0)
          .OrderByDescending(m => m.Answered).ThenByDescending(m => m.Offered).ThenBy(m => m.Extension, StringComparer.Ordinal).ToArray();
        var calls = reply.Calls.Where(c => c.Node == node && c.Queue == queue && c.EnteredMs >= since.ToUnixTimeMilliseconds())
            .OrderByDescending(c => c.EnteredMs)
            .Select(c => new DistributionCall(c.Uniqueid, c.Caller, DateTimeOffset.FromUnixTimeMilliseconds(c.EnteredMs), c.Outcome, c.WaitSeconds,
                c.Attempts.Select(a => new DistributionAttempt(a.Interface, a.Name, DateTimeOffset.FromUnixTimeMilliseconds(a.CalledMs),
                    a.Outcome, a.RingSeconds, a.TalkSeconds)).ToArray())).ToArray();
        return new(Queue(stat, hours), members, calls);
    }

    /// <summary>Same node/search rules as the collector; used for the preview fixture and Queue controller queues.</summary>
    public static void Merge(DistributionReply target, DistributionReply source)
    {
        target.Queues.AddRange(source.Queues); target.Members.AddRange(source.Members); target.Calls.AddRange(source.Calls);
        if (target.StartedAtMs == 0 || source.StartedAtMs != 0 && source.StartedAtMs < target.StartedAtMs) target.StartedAtMs = source.StartedAtMs;
        target.NowMs = Math.Max(target.NowMs, source.NowMs);
    }

    public static DistributionReply Filter(DistributionReply reply, string node, string search, string queue = "")
    {
        bool Has(string value) => search.Length == 0 || value.Contains(search, StringComparison.OrdinalIgnoreCase);
        var keep = reply.Queues.Where(q => (node.Length == 0 || q.Node == node) && (queue.Length == 0 || q.Queue == queue)
                && (Has(q.Queue) || Has(q.Title) || reply.Members.Any(m => m.Node == q.Node && m.Queue == q.Queue && (Has(m.Interface) || Has(m.Name)))))
            .Select(q => (q.Node, q.Queue)).ToHashSet();
        var result = new DistributionReply { StartedAtMs = reply.StartedAtMs, NowMs = reply.NowMs };
        result.Queues.AddRange(reply.Queues.Where(q => keep.Contains((q.Node, q.Queue))));
        result.Members.AddRange(reply.Members.Where(m => keep.Contains((m.Node, m.Queue))));
        result.Calls.AddRange(reply.Calls.Where(c => keep.Contains((c.Node, c.Queue))));
        return result;
    }

    /// <summary>Hour labels for the last N buckets ending at <paramref name="now"/> (local time of the viewer).</summary>
    public static DateTimeOffset[] HourStarts(DateTimeOffset now, int hours)
    {
        var current = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        return Enumerable.Range(0, hours).Select(i => current.AddHours(i - hours + 1)).ToArray();
    }
}
