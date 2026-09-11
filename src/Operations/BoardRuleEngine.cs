using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation only. Input must already have been authorized for the viewer.</summary>
public static class BoardRuleEngine
{
    public static bool IsEndpointKey(string key) =>
        key.StartsWith("SIP/", StringComparison.Ordinal) && key.Length > 4
        || key.StartsWith("PJSIP/", StringComparison.Ordinal) && key.Length > 6
        || key.StartsWith("IAX2/", StringComparison.Ordinal) && key.Length > 5;

    public static string EndpointKey(string key, bool channel)
    {
        var dash = key.LastIndexOf('-');
        return channel && dash > 0 && key.Length - dash == 9 && key[(dash + 1)..].All(Uri.IsHexDigit)
            ? key[..dash] : key;
    }

    public static bool Matches(BoardRule rule, string key, string node) => rule.Enabled
        && (rule.Node.Length == 0 || rule.Node == node)
        && rule.Patterns.Any(p => p.EndsWith('*') ? key.StartsWith(p[..^1], StringComparison.Ordinal) : key == p);

    public static OperatorTile[] Apply(OperatorTile[] tiles, BoardConfiguration configuration)
    {
        // Classification is done per endpoint, not per original portal card: a card
        // may already contain multiple aliases with different display rules.
        var output = new Dictionary<string, (OperatorTile Tile, List<ObservedResource> Rows)>();
        foreach (var tile in tiles)
        {
            // Queue/member keys are not SIP endpoint identifiers. Keep the queue identity.
            if (tile.Kind == EventsPanelCardKind.QUEUE)
            {
                output[tile.Id] = (tile, tile.Rows.ToList());
                continue;
            }
            var parts = tile.Rows.Length > 0
                ? tile.Rows.GroupBy(r => EndpointKey(r.Key, r.Kind == "call"))
                    .Select(g => (Key: g.Key, Rows: g.ToArray()))
                : tile.Keys.DefaultIfEmpty(tile.Title).Select(k => (Key: k, Rows: Array.Empty<ObservedResource>()));
            foreach (var part in parts)
            {
                var rule = configuration.Rules.FirstOrDefault(r => Matches(r, part.Key, tile.Node));
                if (rule?.Kind == "hidden" || (rule is null && !configuration.ShowUnmatched)) continue;
                var id = rule is null ? tile.Id : $"rule|{rule.Id}|{tile.Node}";
                if (!output.TryGetValue(id, out var group))
                {
                    var configured = rule is null ? tile : new OperatorTile(id, rule.Label, tile.Node,
                        rule.Kind == "trunk" ? EventsPanelCardKind.TRUNK : EventsPanelCardKind.PEER, []) { Keys = rule.Patterns, LastSeen = tile.Updated };
                    output[id] = group = (configured, []);
                }
                group.Rows.AddRange(part.Rows);
                if (tile.Updated is { } seen && (group.Tile.LastSeen is null || seen > group.Tile.LastSeen))
                    output[id] = (group.Tile with { LastSeen = seen }, group.Rows);
            }
        }
        return output.Values.Select(g => g.Tile with { Rows = g.Rows.DistinctBy(r => r.Id).ToArray() })
            .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Node, StringComparer.Ordinal).ToArray();
    }
}
