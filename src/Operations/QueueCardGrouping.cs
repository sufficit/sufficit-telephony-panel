using System.Globalization;
using Sufficit.Telephony.Panel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation only: join proven queue identities after authorization and the node filter.</summary>
public static class QueueCardGrouping
{
    public static OperatorTile[] Merge(IEnumerable<OperatorTile> source)
    {
        var result = new List<OperatorTile>();
        var queues = new Dictionary<QueueResourceIdentity, List<OperatorTile>>();
        foreach (var tile in source)
        {
            var identity = QueueResourceIdentity.From(tile);
            // Unknown numbers/names and conflicting ownership cannot prove a shared queue.
            if (tile.Kind != EventsPanelCardKind.QUEUE || identity is null || tile.Rows.Any(row =>
                Guid.TryParse(row.Account, out var owner) && owner != Guid.Empty && owner != identity.ContextId))
            { result.Add(tile); continue; }
            if (!queues.TryGetValue(identity, out var members)) queues[identity] = members = [];
            members.Add(tile);
        }
        result.AddRange(queues.Select(group => Combine(group.Key, group.Value)));
        return result.ToArray();
    }

    private static OperatorTile Combine(QueueResourceIdentity identity, List<OperatorTile> members)
    {
        var preferred = members.OrderByDescending(BoardTotals.CurrentQueue).ThenByDescending(t => t.Updated)
            .ThenBy(t => t.Title, StringComparer.Ordinal).First();
        var id = $"queue-group|{identity.ContextId:N}|{identity.QueueId:N}";
        var rows = members.SelectMany(t => t.Rows).GroupBy(r => (r.Node, r.Id))
            .Select(g => g.OrderByDescending(r => r.Updated).First()).ToArray();
        // The same portal queue may appear under several labels on one node. Count its snapshot once.
        var snapshots = rows.Where(r => r.Kind == "queue" && r.Details.GetValueOrDefault("source") == "queue")
            .GroupBy(r => r.Node).Select(g => g.OrderByDescending(r => r.Updated).First()).ToArray();
        var current = snapshots.Where(r => r.State == "current").ToArray();
        ObservedResource? summary = null;
        if (snapshots.Length > 0)
        {
            var latest = (current.Length > 0 ? current : snapshots).OrderByDescending(r => r.Updated).First();
            var details = new Dictionary<string, string>(latest.Details);
            foreach (var name in new[] { "pending", "connected", "greeting", "offering", "announcing" })
                details[name] = Math.Min(int.MaxValue, current.Sum(r => int.TryParse(r.Details.GetValueOrDefault(name),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? (long)Math.Max(0, count) : 0))
                    .ToString(CultureInfo.InvariantCulture);
            details["correlation"] = current.Any(r => r.Details.GetValueOrDefault("correlation") == "unavailable") ? "unavailable" : "visits";
            summary = latest with { Id = id + "|summary", Node = "", State = current.Length > 0 ? "current" : "stale", Details = details };
        }
        var nodes = members.Select(t => t.Node).Distinct(StringComparer.Ordinal).ToArray();
        return new(id, preferred.Title, nodes.Length == 1 ? nodes[0] : "", EventsPanelCardKind.QUEUE, rows)
        {
            QueueSummary = summary,
            Keys = members.SelectMany(t => t.Keys).Distinct(StringComparer.Ordinal).ToArray(),
            Flows = members.Where(BoardTotals.CurrentQueue).SelectMany(t => t.Flows).DistinctBy(f => f.Id).ToArray(),
            LastSeen = members.Max(t => t.Updated)
        };
    }
}
