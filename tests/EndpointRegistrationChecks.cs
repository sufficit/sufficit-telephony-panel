using System.Text.Json;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class EndpointRegistrationChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.NewGuid(); var id = Guid.NewGuid();
        var registration = new EndpointRegistration(context, id, "0004756200", "6200", "google", now, now.AddMinutes(10), "192.0.2.1", "SIP PBX");
        var card = new EventsPanelCardInfo { Kind = EventsPanelCardKind.PEER, Label = "6200", Channels = ["^SIP/0004756200"] };
        var row = registration.Row(now);
        check(!AuthorizedObservation.Create([card], [row], context).Rows.Any(), "Reproduces missing GUID PJSIP registration on legacy SIP card");
        var cards = EndpointRegistration.Attach([card], [registration]);
        var observation = AuthorizedObservation.Create(cards, [row], context);
        check(cards.Length == 1 && observation.Rows.Length == 1, "Canonical registration attaches to existing SIP card without duplicate");
        check(row.Node == "google-voip" && row.Account == context.ToString("N"), "Registration carries proven tenant and telemetry node");
        check(row.Peer?.Registration == "registered" && row.Peer.Reachability == "unknown" && row.Peer.Device == "unknown", "Registration does not fabricate qualify or free state");
        check(OperatorTile.Describe([row]).Tone == "online", "Idle PJSIP registration appears registered");
        check(registration.Current(now) && !registration.Current(now.AddSeconds(16)), "Registration snapshot freshness bounded independently of expiry");
        check(!(registration with { ExpiresAt = now }).Current(now), "Expired contact is not current");
        check(!(registration with { ObservedAt = now.AddMinutes(1) }).Current(now), "Future observation rejected");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { sub = "reader", authorization = new { schemaVersion = 1, grants = new[] { "telephony.panel:" + context } } }));
        var permissions = CurrentAuthorization.Parse(json.RootElement, "reader");
        check(!AuthorizedObservation.ForCustomer(cards, [row with { Account = Guid.NewGuid().ToString("N") }], permissions, context).Rows.Any(), "Foreign registration cannot enter a customer projection");
        var qualified = row with { Id = "ami", Peer = new PeerEvidence { Reachability = "online", Device = "InUse", RoundTripMilliseconds = 12 }, Updated = now.AddSeconds(1) };
        var combined = EndpointRegistration.Merge([qualified], [registration], now);
        check(combined.Length == 1 && combined[0].Updated == qualified.Updated, "Newer AMI observation cannot supersede combined registration evidence");
        var merged = combined.Single();
        check(merged.Peer?.Registration == "registered" && merged.Peer.Reachability == "online" && merged.Peer.Device == "InUse" && merged.Peer.RoundTripMilliseconds == 12,
            "Snapshot preserves independent AMI qualify, RTT and device state");
    }
}
