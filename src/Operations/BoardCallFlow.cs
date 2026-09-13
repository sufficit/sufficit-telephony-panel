namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation correlation over authorized observations, never over dialed-number guesses.</summary>
public sealed record BoardCallFlow(string Id, string Caller, string Destination, string Phase,
    bool Correlated, ObservedResource[] Channels)
{
    private static string Identity(ObservedResource row) => row.Node + "|" +
        (LiveProjection.Meaningful(row.Details.GetValueOrDefault("linkedid"))
            ? "linked|" + row.Details["linkedid"] : "channel|" + row.Key);

    public static OperatorTile[] Attach(OperatorTile[] tiles, IEnumerable<ObservedResource> authorizedRows)
    {
        var groups = authorizedRows.Where(r => r.Kind is "call" or "waiting").GroupBy(Identity)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var queueNames = tiles.Where(t => t.Kind == Sufficit.Telephony.EventsPanel.EventsPanelCardKind.QUEUE)
            .SelectMany(t => t.Rows.Where(r => r.Kind == "waiting").Select(r => (r.Id, t.Title)))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Title);
        return tiles.Select(tile => tile with
        {
            Flows = tile.Rows.Where(r => tile.Kind == Sufficit.Telephony.EventsPanel.EventsPanelCardKind.QUEUE
                ? r.Kind == "waiting" : r.Kind == "call").GroupBy(Identity).Select(anchor =>
            {
                var related = groups.GetValueOrDefault(anchor.Key, anchor.ToArray());
                // Conflicting explicit accounts cannot establish a shared business-call view.
                var accounts = related.Select(r => Guid.TryParse(r.Account, out var id) ? id : (Guid?)null)
                    .Where(id => id.HasValue).Distinct().ToArray();
                if (accounts.Length > 1) related = anchor.ToArray();
                var ordered = related.OrderByDescending(r => r.Details.GetValueOrDefault("uniqueid") == r.Details.GetValueOrDefault("linkedid")
                        && LiveProjection.Meaningful(r.Details.GetValueOrDefault("linkedid")))
                    .ThenBy(r => r.FirstSeen).ThenBy(r => r.Id, StringComparer.Ordinal).ToArray();
                var caller = ordered.Select(r => r.Caller).FirstOrDefault(LiveProjection.Meaningful) ?? "";
                var queue = related.Where(r => r.Kind == "waiting").OrderByDescending(r => r.Updated).FirstOrDefault();
                var destination = queue is not null ? queueNames.GetValueOrDefault(queue.Id, queue.Details.GetValueOrDefault("queue", ""))
                    : ordered.Select(r => r.Destination).FirstOrDefault(d => LiveProjection.Meaningful(d) && d != caller && d != "s") ?? "";
                return new BoardCallFlow(anchor.Key, caller, destination, queue?.State ?? "channel",
                    ordered.Any(r => LiveProjection.Meaningful(r.Details.GetValueOrDefault("linkedid"))),
                    related.Where(r => r.Kind == "call").DistinctBy(r => r.Id).ToArray());
            }).ToArray()
        }).ToArray();
    }
}
