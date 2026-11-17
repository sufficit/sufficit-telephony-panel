using System.Text.Json;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Streaming;

internal static class DialFailureChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = Guid.NewGuid();
        const string channel = "PJSIP/wss-origin-000000aa";
        JsonElement Event(int cause = 0, string key = channel) => JsonSerializer.SerializeToElement(new
        { channel = key, cause, accountcode = tenant.ToString("N"), linkedid = "private-call", calleridnum = "private-caller", exten = "private-number" });
        foreach (var cause in new[] { 1, 2, 3, 5, 28 })
        {
            var store = new TelemetryStore();
            store.Append("google", "NewChannelEvent", Event(), now);
            store.Append("google", "HangupEvent", Event(cause), now);
            var replica = new TelemetryReplica();
            foreach (var frame in store.Read("", 0).Frames) replica.Apply(frame);
            var rows = replica.Projection.Snapshot(now);
            var failure = rows.Single();
            check(failure.Kind == "call-result" && failure.State == cause.ToString(), "Explicit failure crosses snapshot transport with numeric cause");
            check(failure.Key == "PJSIP/wss-origin" && failure.Account == tenant.ToString("N"), "Failure retains the exact endpoint/node ownership");
            check(failure.Destination == "private-number" && failure.Details.GetValueOrDefault("cause") == cause.ToString(), "Dial result preserves the original dialed destination and numeric Asterisk cause");
            check(failure.Caller is null or "" && !failure.Details.ContainsKey("linkedid"), "Result does not retain caller or linked call metadata");
            var observation = AuthorizedObservation.ForManager(rows);
            var tile = OperatorTile.Create(observation.Cards, observation.ByCard).Single();
            check(tile.FailedDial is not null && tile.Channels.Length == 0 && tile.Flows.Length == 0, "Failure is visible without a live call or flow");
            check(BoardTotals.ActiveCalls([tile]) == 0, "Failure cannot inflate active-call counters");
            var card = new EventsPanelCardInfo { Label = "origin", Kind = EventsPanelCardKind.PEER, Channels = ["^PJSIP/wss-origin"] };
            check(AuthorizedObservation.Create([card], rows, Guid.NewGuid()).Rows.Length == 0, "A matching display rule cannot expose another tenant's failure");
            check(AuthorizedObservation.Create([card], rows.Select(r => r with { Account = null }), tenant).Rows.Length == 0, "Unowned failures cannot enter customer views");
            check(replica.Projection.Snapshot(now + RecentDialFailure.Lifetime).Length == 0, "Failure expires at ten seconds");
        }
        foreach (var cause in new[] { 0, 16, 17, 18, 19, 21, 26, 31 })
        {
            var projection = new LiveProjection();
            projection.Apply("google", "NewChannelEvent", Event(), now);
            projection.Apply("google", "HangupEvent", Event(cause), now);
            check(projection.Snapshot(now).Length == 0, "Normal, busy, no answer and cancellation are not invalid dialing");
        }
        var immediate = new LiveProjection();
        immediate.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Ring", accountcode = tenant.ToString("N") }), now);
        immediate.Apply("google", "HangupEvent", Event(16), now);
        check(immediate.Snapshot(now).Single().State == "attempt-ended", "Immediate PJSIP Ring/normal-clearing attempt remains visible without inventing an invalid number");
        foreach (var state in new[] { "Up", "6", "Down", "unknown" })
        {
            var completed = new LiveProjection();
            completed.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = state }), now);
            completed.Apply("google", "HangupEvent", Event(16), now.AddMilliseconds(100));
            check(completed.Snapshot(now.AddMilliseconds(100)).Length == 0, "Answered/unknown channels cannot be reported as immediate pre-answer attempts");
        }
        var longRing = new LiveProjection();
        longRing.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Ring" }), now);
        longRing.Apply("google", "HangupEvent", Event(16), now.AddSeconds(3));
        check(longRing.Snapshot(now.AddSeconds(3)).Length == 0, "Ordinary ringing cancellation is not an immediate attempt result");
        var transferred = new LiveProjection();
        transferred.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Up" }), now);
        transferred.Apply("google", "NewStateEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Ring" }), now.AddMilliseconds(50));
        transferred.Apply("google", "HangupEvent", Event(16), now.AddMilliseconds(100));
        check(transferred.Snapshot(now.AddMilliseconds(100)).Length == 0, "Earlier Up evidence excludes short answered transfers");
        var routed = new LiveProjection();
        routed.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Ring", exten = "*0042#" }), now);
        routed.Apply("google", "NewStateEvent", JsonSerializer.SerializeToElement(new { channel, channelstate = "Ring", connectedlinenum = "another-destination", exten = "h" }), now);
        routed.Apply("google", "HangupEvent", Event(16), now);
        var original = routed.Snapshot(now).Single();
        check(original.State == "attempt-ended" && original.Destination == "*0042#" && original.Details["cause"] == "16", "Immediate attempt retains the original dial string and normal-clearing cause despite later routing updates");
        foreach (var placeholder in new[] { "", "s", "h", "unknown", "<unknown>" })
        {
            var missing = new LiveProjection();
            missing.Apply("google", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel, exten = placeholder }), now);
            missing.Apply("google", "HangupEvent", Event(28), now);
            check(missing.Snapshot(now).Single().Destination is null, "Missing or technical extensions do not become a fabricated dialed number");
        }
        var next = new LiveProjection();
        next.Apply("google", "NewChannelEvent", Event(), now);
        next.Apply("google", "HangupEvent", Event(28), now);
        next.Apply("other", "NewChannelEvent", Event(key: "PJSIP/wss-origin-000000bb"), now);
        check(next.Snapshot(now).Any(r => r.Kind == "call-result"), "A different node cannot clear the failure");
        next.Apply("google", "NewChannelEvent", Event(key: "PJSIP/wss-origin-000000cc"), now);
        check(!next.Snapshot(now).Any(r => r.Kind == "call-result"), "The endpoint's next attempt clears its prior result");
        check(!TelemetryStore.AllowedFields.Contains("causetxt"), "Free-form cause text remains excluded");
    }
}
