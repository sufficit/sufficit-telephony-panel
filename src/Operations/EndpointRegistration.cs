using Sufficit.Telephony.Panel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Safe, tenant-owned registration snapshot from the API; never reachability or device use.</summary>
public sealed record EndpointRegistration(Guid ContextId, Guid EndpointId, string Title, string Label,
    string Node, DateTimeOffset ObservedAt, DateTimeOffset ExpiresAt, string? Address, string? UserAgent)
{
    public bool Current(DateTimeOffset now) => ContextId != Guid.Empty && EndpointId != Guid.Empty
        && ObservedAt >= now.AddSeconds(-15) && ObservedAt <= now.AddSeconds(2) && ExpiresAt > now;
    public string PeerKey => $"PJSIP/{EndpointId:N}";
    public ObservedResource Row(DateTimeOffset now)
    {
        var node = Node is "google" or "eveo" or "apoint" ? Node + "-voip" : Node;
        return new($"registration|{node}|{EndpointId:N}", node, "peer", PeerKey, "Registered", null, null,
            ContextId.ToString("N"), ObservedAt, ObservedAt,
            new Dictionary<string, string> { ["source"] = "pjsip-registration", ["address"] = Address ?? "",
                ["useragent"] = UserAgent ?? "", ["expires"] = ExpiresAt.ToString("O") })
        { Peer = new PeerEvidence { Registration = "registered", RegistrationObservedAt = ObservedAt,
            LastRegisteredObservedAt = ObservedAt, LastEventType = "PjsipRegistrationSnapshot" } };
    }
    public static ObservedResource[] Merge(IEnumerable<ObservedResource> source, IEnumerable<EndpointRegistration> registrations, DateTimeOffset now)
    {
        var rows = source.ToArray();
        var additions = registrations.Select(registration => {
            var row = registration.Row(now);
            var prior = rows.Where(r => r.Node == row.Node && r.Key == row.Key && r.Kind == "peer" && r.Peer is not null)
                .OrderByDescending(r => r.Updated).FirstOrDefault();
            return prior is null ? row : row with { Updated = prior.Updated > row.Updated ? prior.Updated : row.Updated, Peer = prior.Peer! with { Registration = "registered",
                RegistrationObservedAt = registration.ObservedAt, LastRegisteredObservedAt = registration.ObservedAt } };
        }).ToArray();
        var replaced = additions.Select(r => (r.Node, r.Key)).ToHashSet();
        return rows.Where(r => r.Kind != "peer" || !replaced.Contains((r.Node, r.Key))).Concat(additions).ToArray();
    }
    public static EventsPanelCardInfo[] Attach(EventsPanelCardInfo[] cards, IEnumerable<EndpointRegistration> registrations)
    {
        var result = cards.ToList();
        foreach (var registration in registrations.DistinctBy(r => r.EndpointId))
        {
            // Exact catalogue key is proof of ownership; labels and extension suffixes are not.
            var matches = result.Where(c => c.Kind == EventsPanelCardKind.PEER && c.Channels.Any(key =>
                key == "^SIP/" + registration.Title || key == "^PJSIP/" + registration.Title || key == "^" + registration.PeerKey)).ToArray();
            if (matches.Length > 1) continue;
            if (matches.Length == 0)
                result.Add(new() { Kind = EventsPanelCardKind.PEER, Label = registration.Label, Channels = ["^" + registration.PeerKey] });
            else if (!matches[0].Channels.Contains("^" + registration.PeerKey))
            {
                var source = matches[0];
                result[result.IndexOf(source)] = new() { Kind = source.Kind, Label = source.Label,
                    Channels = [.. source.Channels, "^" + registration.PeerKey] };
            }
        }
        return result.ToArray();
    }
}
