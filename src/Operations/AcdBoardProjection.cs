using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Explicit ACD evidence, not native AMI queue events. Correlation is node/context/channel exact.</summary>
public sealed class AcdBoardProjection
{
    public Dictionary<EventsPanelCardInfo, List<ObservedResource>> ByCard { get; } = new();
    public ObservedResource[] Rows => ByCard.Values.SelectMany(r => r).ToArray();
    public static AcdBoardProjection Create(IEnumerable<AcdBoardSample> samples, IEnumerable<ObservedResource> channels,
        Guid? context, DateTimeOffset now)
    {
        var result = new AcdBoardProjection();
        var candidates = channels.Where(r => r.Kind == "call").ToArray();
        foreach (var sample in samples.Where(s => context is null || s.ContextId == context))
        {
            if (sample.Snapshot is not { } snapshot || !Guid.TryParseExact(snapshot.ContextId, "N", out var tenant) || tenant != sample.ContextId) continue;
            var fresh = sample.Available && snapshot.Fresh(now);
            foreach (var queue in snapshot.Queues)
            {
                var key = $"acd:{tenant:N}:{queue.QueueId}";
                var card = new EventsPanelCardInfo { Label = queue.Title, Kind = EventsPanelCardKind.QUEUE, Channels = [key] };
                var metadata = new Dictionary<string, string>
                {
                    ["source"] = "acd", ["queue"] = key, ["queueid"] = queue.QueueId, ["contextid"] = tenant.ToString("N"),
                    ["nodeid"] = snapshot.NodeId, ["correlation"] = snapshot.Visits is null ? "unavailable" : "visits"
                };
                if (fresh)
                {
                    metadata["pending"] = queue.Waiting.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["connected"] = queue.Connected.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["greeting"] = queue.Greeting.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["offering"] = queue.Offering.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    metadata["announcing"] = queue.Announcing.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                var rows = new List<ObservedResource>
                {
                    new($"{sample.Node}|{key}", sample.Node, "queue", key, fresh ? "current" : "stale", null, null,
                        tenant.ToString("N"), snapshot.ObservedAt, snapshot.ObservedAt, metadata)
                };
                result.ByCard.Add(card, rows);
                if (!fresh) continue;
                foreach (var visit in (snapshot.Visits ?? []).Where(v => v.QueueId == queue.QueueId))
                {
                    var matches = candidates.Where(r => r.Node == sample.Node && Guid.TryParse(r.Account, out var owner) && owner == tenant &&
                        !string.IsNullOrWhiteSpace(visit.ChannelId) && r.Details.GetValueOrDefault("uniqueid") == visit.ChannelId &&
                        (string.IsNullOrWhiteSpace(visit.ChannelName) || r.Key == visit.ChannelName)).ToArray();
                    var channel = matches.Length == 1 ? matches[0] : null;
                    var details = new Dictionary<string, string>(metadata)
                    {
                        ["visitid"] = visit.Id, ["channelid"] = visit.ChannelId ?? "",
                        ["phase"] = visit.Phase, ["correlation"] = channel is null ? "unmatched" : "exact-channel"
                    };
                    if (channel is not null)
                    {
                        details["uniqueid"] = channel.Details.GetValueOrDefault("uniqueid", "");
                        details["linkedid"] = channel.Details.GetValueOrDefault("linkedid", "");
                    }
                    rows.Add(new($"{sample.Node}|{key}|{visit.Id}", sample.Node, "waiting", channel?.Key ?? $"{key}:visit:{visit.Id}",
                        visit.Phase == "connected" ? "answered" : visit.Phase, channel?.Caller ?? visit.Caller, queue.Title,
                        tenant.ToString("N"), visit.EnteredAt, snapshot.ObservedAt, details));
                }
            }
        }
        return result;
    }
}
