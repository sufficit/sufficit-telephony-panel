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
        check(day.Calls.Length == 18 && day.Calls[0].EnteredAt >= day.Calls[^1].EnteredAt, "Latest calls first");
        check(hour.Calls.All(c => c.EnteredAt >= now.AddHours(-1).AddSeconds(-1)), "Calls follow the window");
        check(QueueDistributionView.Detail(reply, "google-voip", "missing", 24) is null, "Unknown queue has no detail");
        var starts = QueueDistributionView.HourStarts(new DateTimeOffset(2026, 10, 22, 14, 37, 0, TimeSpan.FromHours(-3)), 4);
        check(starts[^1].Hour == 14 && starts[0].Hour == 11 && starts[0].Minute == 0, "Hour labels end at the current hour");
    }
}
