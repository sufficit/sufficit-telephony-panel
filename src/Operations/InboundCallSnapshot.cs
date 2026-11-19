using Sufficit.Telephony.Panel;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record InboundCallSnapshot(Guid ContextId, Guid DidId, string Node, string Channel,
    string UniqueId, string LinkedId, string State, string? Caller, string? Destination, DateTimeOffset ObservedAt)
{
    public bool DidAnchor { get; init; } = true;
    public Guid? ReceiverEndpointId { get; init; }
    public static bool MatchesEndpoint(EventsPanelCardInfo card, ObservedResource row) => card.Kind == EventsPanelCardKind.PEER
        && row.Kind == "call" && row.Details.GetValueOrDefault("source") == "did-inbound"
        && Guid.TryParseExact(row.Details.GetValueOrDefault("receiver-endpoint"), "N", out var endpoint) && endpoint != Guid.Empty
        && card.Channels.Contains($"^PJSIP/{endpoint:N}");
    public bool Fresh(DateTimeOffset now) => ObservedAt >= now.AddSeconds(-8) && ObservedAt <= now.AddSeconds(2);
    public ObservedResource Row()
    {
        var node = Node.EndsWith(".sufficit.com.br", StringComparison.Ordinal) ? Node[..^".sufficit.com.br".Length] : Node;
        if (node is "google" or "eveo" or "apoint") node += "-voip";
        return new($"{node}|call|{Channel}", node, "call", Channel, State, Caller, Destination,
            ContextId.ToString("N"), ObservedAt, ObservedAt, new Dictionary<string,string> {
                ["source"] = "did-inbound", ["didid"] = DidId.ToString("N"),
                ["uniqueid"] = UniqueId, ["linkedid"] = LinkedId,
                ["did-anchor"] = DidAnchor ? "true" : "false", ["receiver-endpoint"] = ReceiverEndpointId?.ToString("N") ?? "" });
    }
    public static ObservedResource[] IdleEvidence(EventsPanelCardInfo[] cards, InboundCallSource[] sources, Guid context, DateTimeOffset now)
    {
        if (sources.Length == 0 || sources.Any(s => !s.Fresh(now))) return [];
        var observed = sources.Min(s => s.ObservedAt);
        return cards.Where(c => InboundCatalogueEntry.State(c.Channels) is not null).Select(c => c.Channels.Single().Split(':'))
            .Where(p => p[1] == "did" && Guid.ParseExact(p[2], "N") == context)
            .Select(p => new ObservedResource($"inbound|{context:N}|{p[3]}", "", "inbound", string.Join(':', p), "current", null, null,
                context.ToString("N"), observed, observed, new Dictionary<string,string> { ["source"] = "did-inbound", ["didid"] = p[3] })).ToArray();
    }
    public static ObservedResource[] Merge(IEnumerable<ObservedResource> source, IEnumerable<InboundCallSnapshot> calls)
    {
        var original = source.ToArray();
        var additions = calls.Select(c => {
            var row = c.Row();
            var prior = original.FirstOrDefault(r => r.Node == row.Node && r.Key == row.Key && r.Kind == "call"
                && r.Details.GetValueOrDefault("uniqueid") == c.UniqueId);
            return prior is null ? row : row with { FirstSeen = prior.FirstSeen };
        }).ToArray();
        var keys = additions.Select(c => (c.Node, c.Key)).ToHashSet();
        return original.Where(r => r.Kind != "call" || !keys.Contains((r.Node, r.Key))).Concat(additions).ToArray();
    }
}
public sealed record InboundCallSource(string Node, bool Available, DateTimeOffset ObservedAt, InboundCallSnapshot[] Calls)
{
    public bool Fresh(DateTimeOffset now) => Available && ObservedAt >= now.AddSeconds(-8) && ObservedAt <= now.AddSeconds(2);
}
