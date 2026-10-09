using Sufficit.Telephony.EventsPanel;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Tenant-filtered view with a bounded comparison budget; never expose the raw manager stream.</summary>
public sealed class AuthorizedObservation
{
    public static AuthorizedObservation ForCustomer(EventsPanelCardInfo[] cards,
        IEnumerable<ObservedResource> source, CurrentAuthorization permissions, Guid? context)
    {
        context = permissions.SelectContext(context);
        // Unknown ownership and foreign accounts are never exposed. In particular,
        // matching a display regex is not proof that a shared trunk belongs here.
        var rows = source.Where(row => Guid.TryParse(row.Account, out var account)
            && permissions.CanRead(account) && (!context.HasValue || account == context)).ToArray();
        if (context is { } selected) return Create(cards, rows, selected);
        var result = new AuthorizedObservation { Rows = rows };
        foreach (var group in rows.GroupBy(row => Guid.Parse(row.Account!)))
        {
            var scoped = ForManager(group);
            foreach (var pair in scoped.ByCard) result.ByCard.Add(pair.Key, pair.Value);
            foreach (var pair in scoped.Labels) result.Labels[pair.Key] = pair.Value;
            result.Cards = result.Cards.Concat(scoped.Cards).ToArray();
        }
        return result;
    }
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
    // Only the manager-authorized private reader may supply this projection.
    public void AddAcd(AcdBoardProjection projection)
    {
        foreach (var entry in projection.ByCard)
        {
            ByCard.Add(entry.Key, entry.Value);
            foreach (var row in entry.Value) Labels[row.Id] = entry.Key.Label;
        }
        Cards = Cards.Concat(projection.ByCard.Keys).ToArray();
        Rows = Rows.Concat(projection.Rows).ToArray();
    }
    public static AuthorizedObservation Create(EventsPanelCardInfo[] cards, IEnumerable<ObservedResource> source, Guid context)
    {
        var result = new AuthorizedObservation { Cards = cards };
        var rows = new List<ObservedResource>();
        var comparisons = 0;
        var candidates = source.OrderByDescending(r => r.Updated).Take(LiveProjection.Capacity).ToArray();
        foreach (var row in candidates)
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
        // Preserve internal legs for diagnostics only after an authorized resource anchors
        // the call. Each added leg still needs its own matching tenant AccountCode.
        var calls = rows.Where(r => r.Kind is "call" or "waiting")
            .Where(r => LiveProjection.Meaningful(r.Details.GetValueOrDefault("linkedid")))
            .Select(r => (r.Node, Linked: r.Details["linkedid"])).ToHashSet();
        var included = rows.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var row in candidates.Where(r => r.Kind == "call"))
            if (!included.Contains(row.Id) && Guid.TryParse(row.Account, out var account) && account == context
                && calls.Contains((row.Node, row.Details.GetValueOrDefault("linkedid", "")))) rows.Add(row);
        result.Rows = rows.ToArray();
        return result;
    }
}
