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
            check(failure.Caller is null or "" && failure.Destination is null or "" && failure.Details.Count == 0, "Failure retains no caller, destination or linked call metadata");
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
