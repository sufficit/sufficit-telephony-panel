using Grpc.Core;
using Sufficit.Telephony.Streaming;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.EventsPanel;

// Read only the user's 00000060xx endpoints. Never log callers, tokens or raw events.
using var channel = UnixTelemetryChannel.Create("/run/sufficit-ami-observation/telemetry.sock");
var client = new Telemetry.TelemetryClient(channel);
using var call = client.Watch(new WatchRequest(), deadline: DateTime.UtcNow.AddSeconds(10));
var replica = new TelemetryReplica();
while (await call.ResponseStream.MoveNext(default))
{
    var frame = call.ResponseStream.Current;
    replica.Apply(frame);
    if (frame.Resource is { Node: "google-voip", Kind: "peer" } row
        && (row.Key.StartsWith("SIP/00000060", StringComparison.Ordinal) || row.Key.StartsWith("PJSIP/00000060", StringComparison.Ordinal)))
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { row.Key, row.Kind, row.Account, row.State,
            Updated = DateTimeOffset.FromUnixTimeMilliseconds(row.UpdatedMs) }));
    if (frame.End is not null) break;
}
var peer = replica.Projection.Snapshot(DateTimeOffset.UtcNow).Single(row => row.Node == "google-voip" && row.Kind == "peer" && row.Key == "SIP/0000006007");
var tile = new OperatorTile("probe", "probe", peer.Node, EventsPanelCardKind.PEER, [peer]) { Keys = ["^SIP/0000006007"] };
var target = EndpointSupervisionTarget.For(tile, Guid.Parse("d21cfb04-9d37-473b-837c-67591a26feed"), true, DateTimeOffset.UtcNow).Single();
if (target.Prefix != "SIP/0000006007-" || !target.Observed([peer], true, DateTimeOffset.UtcNow)) throw new InvalidOperationException("Live idle peer rejected");
Console.WriteLine("PASS: actual Google idle peer resolves SIP/0000006007- in supplied Sufficit context; no call originated and no user authorization impersonated.");
