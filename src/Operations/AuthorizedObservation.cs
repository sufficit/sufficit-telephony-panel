using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Tenant-filtered view with a bounded comparison budget; never expose the raw manager stream.</summary>
public sealed class AuthorizedObservation
{
    /// <summary>Only for the upstream manager ALL stream, after server-side role validation.</summary>
    public static AuthorizedObservation ForManager(IEnumerable<ObservedResource> source)
    {
        var result = new AuthorizedObservation { Rows = source.OrderByDescending(r => r.Updated).Take(LiveProjection.Capacity).ToArray() };
        var cards = new Dictionary<string, EventsPanelCardInfo>(StringComparer.Ordinal);
        foreach (var row in result.Rows)
        {
            var queue = row.Kind is "queue" or "waiting" or "agent";
            var key = queue ? row.Details.GetValueOrDefault("queue", row.Key) : row.Key;
            // Internal channel legs are diagnostic rows, not endpoint identities.
            // Keep them in Rows for call correlation and the Channels tab, without
            // generating standalone extension cards (e.g. Local/...;1 and ;2).
            if (!queue && !BoardRuleEngine.IsEndpointKey(key)) continue;
            key = BoardRuleEngine.EndpointKey(key, row.Kind == "call");
            var id = (queue ? "queue|" : "peer|") + key;
            if (!cards.TryGetValue(id, out var card))
            {
                card = new() { Label = key, Kind = queue ? EventsPanelCardKind.QUEUE : EventsPanelCardKind.PEER, Channels = [key] };
                cards[id] = card; result.ByCard[card] = [];
            }
            result.ByCard[card].Add(row);
            result.Labels[row.Id] = card.Label;
        }
        result.Cards = cards.Values.ToArray();
        return result;
    }
    public EventsPanelCardInfo[] Cards { get; private set; } = [];
    public const int ComparisonLimit = 100_000;
    public Dictionary<EventsPanelCardInfo, List<ObservedResource>> ByCard { get; } = new();
    public Dictionary<string, string> Labels { get; } = new();
    public ObservedResource[] Rows { get; private set; } = [];
    public bool Limited { get; private set; }
    public static AuthorizedObservation Create(EventsPanelCardInfo[] cards, IEnumerable<ObservedResource> source, Guid context)
    {
        var result = new AuthorizedObservation();
        var rows = new List<ObservedResource>();
        var comparisons = 0;
        foreach (var row in source.OrderByDescending(r => r.Updated))
        {
            if (row.Kind is "call" or "waiting" && (!Guid.TryParse(row.Account, out var account) || account != context)) continue;
            var matched = false;
            foreach (var card in cards)
            {
                if (++comparisons > ComparisonLimit) { result.Limited = true; break; }
                if (!LiveProjection.Matches(card, row)) continue;
                matched = true;
                if (!result.ByCard.TryGetValue(card, out var list)) result.ByCard[card] = list = [];
                list.Add(row); result.Labels.TryAdd(row.Id, card.Label);
            }
            if (matched) rows.Add(row);
            if (result.Limited) break;
        }
        result.Rows = rows.ToArray();
        return result;
    }
}
