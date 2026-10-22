using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Streaming;

internal static class QueueDistributionChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(QueueDistributionView.Extension("Local/0007366001@from-queue/n") == "0007366001", "Local member shows the extension");
        check(QueueDistributionView.Extension("PJSIP/6001") == "6001" && QueueDistributionView.Extension("SIP/abc-0001") == "abc", "SIP/PJSIP member extension");
        var now = DateTimeOffset.UtcNow;
        var reply = QueueDistributionFixture.Create(now);
        var queues = QueueDistributionView.Queues(reply, 24);
        check(queues.Length == 2 && queues[0].Queue == "0007366500", "Busiest queue first");
        var day = QueueDistributionView.Detail(reply, "google-voip", "0007366500", 24)!;
        var hour = QueueDistributionView.Detail(reply, "google-voip", "0007366500", 1)!;
        check(day.Queue.EnteredByHour.Length == 24 && hour.Queue.EnteredByHour.Length == 1, "Window selects hourly buckets");
        check(hour.Queue.Entered == reply.Queues[0].Entered[^1], "One hour = current bucket");
        check(day.Members.First().Answered >= day.Members.Last().Answered, "Members ordered by answers");
        var offline = day.Members.Single(m => m.Extension == "0007366001");
        check(offline.Unavailable > 0 && offline.AnswerRate is null && offline.Cancelled == 0, "Unavailable member has no answer rate");
        var busy = day.Members.Single(m => m.Extension == "0007366004");
        check(busy.Label == "BRANDÃO SERVI - 6004" && busy.AverageTalk == 185, "Member label and average talk");
        check(busy.Cancelled == busy.Offered - busy.Answered - busy.Missed, "Rang while another answered");
        check(day.Calls.Length == 19 && day.Calls.Count(c => c.Outcome == "waiting") == 1 && day.Calls[0].EnteredAt >= day.Calls[^1].EnteredAt, "Latest calls first");
        check(hour.Calls.All(c => c.EnteredAt >= now.AddHours(-1).AddSeconds(-1)), "Calls follow the window");
        check(QueueDistributionView.Detail(reply, "google-voip", "missing", 24) is null, "Unknown queue has no detail");
        check(OperatorTile.ContactHost("sip:6001@177.1.2.3:5060;transport=udp") == "177.1.2.3:5060" && OperatorTile.ContactHost("sip:10.0.0.9:5060") == "10.0.0.9:5060", "Contact URI shows host and port");
        // Queue controller (new queues): per-offer events shaped like the collector.
        var context = Guid.NewGuid(); var queueId = Guid.NewGuid(); var visit = Guid.NewGuid(); var lost = Guid.NewGuid();
        var t = now.AddMinutes(-10);
        QueueDiagnosticEvent E(long id, int s, string kind, string ext, string? reason = null) => new(id, t.AddSeconds(s), kind, ext, reason);
        var calls = new[] { new QueueDiagnosticCall(visit, queueId, "ch1", "completed", t, t.AddSeconds(12), t.AddSeconds(132)),
                            new QueueDiagnosticCall(lost, queueId, "ch2", "abandoned", t.AddMinutes(1), null, t.AddMinutes(1).AddSeconds(20)) };
        var events = new Dictionary<Guid, QueueDiagnosticEvent[]>
        {
            [visit] = [E(1, 1, "offered", "6001"), E(2, 1, "offered", "6002"), E(3, 1, "offered", "6003"), E(4, 2, "ringing", "6001"), E(5, 2, "ringing", "6002"),
                       E(6, 2, "offer_finished", "6003", "cancelled_or_unavailable"), E(7, 12, "connected", "6002"), E(8, 12, "offer_finished", "6001", "cancelled_or_unavailable"), E(9, 132, "offer_finished", "6002", "completed")],
            [lost] = [E(10, 61, "offered", "6001"), E(11, 62, "ringing", "6001"), E(12, 77, "offer_finished", "6001", "ring_timeout"),
                      E(13, 61, "offered", "6004"), E(14, 62, "ringing", "6004"), E(15, 62, "offer_finished", "6004", "channel_ended"),
                      E(16, 61, "offered", "6005"), E(17, 62, "ringing", "6005"), E(18, 70, "offer_finished", "6005", "channel_ended")],
        };
        var built = ControllerQueueDistribution.Build(context, "google-voip", calls, events, new Dictionary<Guid, string> { [queueId] = "Suporte" }, now);
        var bq = built.Queues.Single();
        check(bq.Title == "Suporte" && bq.Entered.Sum() == 2 && bq.Answered.Sum() == 1 && bq.Abandoned.Sum() == 1 && bq.Account == context.ToString("N"), "Controller queue totals with title and company");
        var m1 = built.Members.Single(m => m.Interface == "6001"); var m2 = built.Members.Single(m => m.Interface == "6002"); var m3 = built.Members.Single(m => m.Interface == "6003");
        check(m2.Answered.Sum() == 1 && m2.RingSeconds.Sum() == 10 && m2.TalkSeconds.Sum() == 120, "Controller answer with ring and talk time");
        check(m1.Offered.Sum() == 2 && m1.Missed.Sum() == 1 && m1.Answered.Sum() == 0, "Ring timeout counts as not answered");
        check(m3.Unavailable.Sum() == 1, "Offer that never rang counts as unavailable");
        check(built.Members.Single(m => m.Interface == "6004").Unavailable.Sum() == 1 && built.Members.Single(m => m.Interface == "6005").Missed.Sum() == 1, "Channel ended at once = unavailable; later = declined");
        check(built.Calls.First(c => c.Uniqueid == visit.ToString("N")).Attempts.Single(a => a.Interface == "6001").Outcome == "other-answered", "Ringing member when another answered");
        var detailNew = QueueDistributionView.Detail(built, "google-voip", queueId.ToString("N"), 24)!;
        check(QueueDistributionView.Queues(built, 24)[0].Label == "Suporte" && detailNew.Members.Length == 5, "Controller queue in the view");
        check(QueueDistributionView.Filter(built, "", "suporte").Queues.Count == 1 && QueueDistributionView.Filter(built, "", "6002").Queues.Count == 1, "Search by title and extension");
        check(QueueDistributionView.StrategyLabel("ringall") == "Ring all at once" && QueueDistributionView.StrategyLabel("custom") == "custom", "Strategy labels");
        var configuration = new QueueConfigurationSet(context, Guid.NewGuid(), [new(queueId.ToString("N"), "Suporte", true, 20, 60, "ringall", 3, 15, 1000000, [new(30, 5)],
            [new("6001", "Ana", 0, false, true, true, false, 1)])]);
        check(QueueDiagnosticsReader.Valid(configuration, context, configuration.NodeId) && !QueueDiagnosticsReader.Valid(configuration, Guid.NewGuid(), configuration.NodeId), "Configuration bound to its context");
        var starts = QueueDistributionView.HourStarts(new DateTimeOffset(2026, 10, 22, 14, 37, 0, TimeSpan.FromHours(-3)), 4);
        check(starts[^1].Hour == 14 && starts[0].Hour == 11 && starts[0].Minute == 0, "Hour labels end at the current hour");
    }
}
