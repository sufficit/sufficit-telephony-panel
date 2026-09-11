namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Local-node correlation only, over an already authorized channel snapshot.</summary>
public sealed record ObservedCallGroup(string Node, string LinkedId, ObservedResource[] Channels)
{
    public bool Correlated => LinkedId.Length != 0;
    public DateTimeOffset FirstSeen => Channels.Min(c => c.FirstSeen);
    public DateTimeOffset Updated => Channels.Max(c => c.Updated);

    // No number/name heuristic and no cross-server correlation. Missing Linkedid is
    // an independent *unclassified channel*, not proof of a separate business call.
    public static ObservedCallGroup[] Create(IEnumerable<ObservedResource> authorizedRows) => authorizedRows
        .Where(r => r.Kind == "call")
        .GroupBy(r => (r.Node, Linked: r.Details.GetValueOrDefault("linkedid", "").Trim(),
            Unknown: string.IsNullOrWhiteSpace(r.Details.GetValueOrDefault("linkedid")) ? r.Id : ""))
        .Select(g => new ObservedCallGroup(g.Key.Node, g.Key.Linked,
            g.OrderBy(c => c.FirstSeen).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray()))
        .OrderByDescending(g => g.FirstSeen).ThenBy(g => g.Node, StringComparer.Ordinal)
        .ThenBy(g => g.LinkedId, StringComparer.Ordinal).ToArray();

    // Filter groups, not their members: the matching leg must not hide related legs.
    public static ObservedCallGroup[] Filter(IEnumerable<ObservedCallGroup> groups,
        Func<ObservedResource, bool> matches) => groups.Where(g => g.Channels.Any(matches)).ToArray();
}
