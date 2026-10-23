using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Synthetic distribution for preview mode only (Development + ?preview=true).</summary>
public static class QueueDistributionFixture
{
    public static readonly Guid Context = Guid.Parse("11111111-1111-4111-8111-111111111111");

    public static DistributionReply Create(DateTimeOffset now)
    {
        var account = Context.ToString("N");
        var reply = new DistributionReply { StartedAtMs = now.AddHours(-30).ToUnixTimeMilliseconds(), NowMs = now.ToUnixTimeMilliseconds() };
        long[] Hours(Func<int, long> value) => Enumerable.Range(0, QueueDistribution.Hours).Select(value).ToArray();
        // Business hours shape: busier from 08h to 18h local.
        long Load(int i) { var hour = now.AddHours(i - 23).ToLocalTime().Hour; return hour is >= 8 and < 18 ? 6 + (hour * 7 + i) % 9 : (i % 5 == 0 ? 1 : 0); }
        var queue = new QueueStat { Node = "google-voip", Queue = "0007366500", Account = account };
        queue.Entered.AddRange(Hours(Load));
        queue.Answered.AddRange(Hours(i => Math.Max(0, Load(i) - (Load(i) > 6 ? 2 : 0))));
        queue.Abandoned.AddRange(Hours(i => Load(i) > 6 ? 1 : 0));
        queue.WaitSeconds.AddRange(Hours(i => Load(i) * 14));
        reply.Queues.Add(queue);
        var second = new QueueStat { Node = "google-voip", Queue = "0007366501", Account = account };
        second.Entered.AddRange(Hours(i => Load(i) / 3)); second.Answered.AddRange(Hours(i => Load(i) / 3));
        second.Abandoned.AddRange(Hours(_ => 0)); second.WaitSeconds.AddRange(Hours(i => Load(i) * 3));
        reply.Queues.Add(second);
        (string Ext, string Name, double Share, double Miss, bool Offline)[] members =
        [
            ("6004", "BRANDÃO SERVI - 6004", .34, .10, false), ("6008", "RC SERVICOS - 6008", .28, .15, false),
            ("6007", "RC SERVICOS - 6007", .2, .25, false), ("6006", "Andreza Cristiane Debora - 6006", .12, .4, false),
            ("6003", "BRANDÃO SERVI - 6003", .06, .55, false), ("6001", "BRANDÃO SERVI - 6001", 0, 0, true),
        ];
        foreach (var m in members)
        {
            var stat = new MemberStat { Node = "google-voip", Queue = "0007366500", Account = account,
                Interface = $"Local/000736{m.Ext}@from-queue/n", Name = m.Name,
                LastAnsweredMs = m.Offline ? 0 : now.AddMinutes(-7 - (m.Ext[^1] - '0') * 3).ToUnixTimeMilliseconds() };
            long Answered(int i) => (long)Math.Round(Math.Max(0, Load(i) - (Load(i) > 6 ? 2 : 0)) * m.Share);
            long Missed(int i) => (long)Math.Round(Load(i) * m.Miss);
            stat.Offered.AddRange(Hours(i => Load(i)));
            stat.Answered.AddRange(Hours(Answered));
            stat.Missed.AddRange(Hours(Missed));
            stat.Unavailable.AddRange(Hours(i => m.Offline ? Load(i) : 0));
            stat.Busy.AddRange(Hours(i => m.Ext == "6008" ? Load(i) / 3 : 0));
            stat.RingSeconds.AddRange(Hours(i => (Answered(i) + Missed(i)) * (m.Miss > .3 ? 13 : 6)));
            stat.TalkSeconds.AddRange(Hours(i => Answered(i) * 185));
            reply.Members.Add(stat);
        }
        string[] callers = ["21987651234", "21999012345", "1133224455", "21976543210", "4733221100"];
        for (var c = 0; c < 18; c++)
        {
            var entered = now.AddMinutes(-c * 11 - 2);
            var outcome = c % 7 == 3 ? "abandoned" : "answered";
            var trace = new CallTrace { Node = "google-voip", Queue = "0007366500", Account = account, Uniqueid = $"demo.{c}",
                Caller = callers[c % callers.Length], EnteredMs = entered.ToUnixTimeMilliseconds(), Outcome = outcome, WaitSeconds = 6 + c % 5 * 4 };
            var winner = outcome == "answered" ? members[c % 4].Ext : "";
            foreach (var m in members)
                trace.Attempts.Add(new CallAttempt { Interface = $"Local/000736{m.Ext}@from-queue/n", Name = m.Name,
                    CalledMs = entered.AddSeconds(1).ToUnixTimeMilliseconds(),
                    Outcome = m.Offline ? "unavailable" : m.Ext == winner ? "answered" : outcome == "abandoned" ? "caller-hangup" : "other-answered",
                    RingSeconds = m.Ext == winner ? trace.WaitSeconds - 1 : 0, TalkSeconds = m.Ext == winner ? 140 + c * 9 : 0 });
            reply.Calls.Add(trace);
        }
        // One call waiting right now, ringing two members (one offline).
        var live = new CallTrace { Node = "google-voip", Queue = "0007366500", Account = account, Uniqueid = "demo.live", Caller = "21988887777",
            EnteredMs = now.AddSeconds(-42).ToUnixTimeMilliseconds(), Outcome = "waiting" };
        foreach (var (ext, outcome) in new[] { ("6004", "ringing"), ("6007", "ringing"), ("6008", "busy"), ("6001", "unavailable") })
            live.Attempts.Add(new CallAttempt { Interface = $"Local/000736{ext}@from-queue/n", Name = "Ramal " + ext, CalledMs = now.AddSeconds(-30).ToUnixTimeMilliseconds(), Outcome = outcome });
        reply.Calls.Insert(0, live);
        return reply;
    }
}
