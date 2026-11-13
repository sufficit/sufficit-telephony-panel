using System.Globalization;

namespace Sufficit.Telephony.Panel.Operations;

public static class QueueCardOrder
{
    public static OperatorTile[] Sort(IEnumerable<OperatorTile> source, bool byUtilization) => source
        .OrderByDescending(t => byUtilization ? Utilization(t) : t.HasObservedActivity ? 1L : 0L)
        .ThenByDescending(t => byUtilization ? BoardTotals.Waiting([t]) : 0)
        .ThenByDescending(BoardTotals.CurrentQueue).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
        .ThenBy(t => t.Node, StringComparer.Ordinal).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();

    private static long Utilization(OperatorTile tile)
    {
        if (tile.Queue is { } queue)
            return queue.State == "current" ? Count(queue, "pending") + Count(queue, "connected") : 0;
        return tile.QueueCalls.DistinctBy(r => r.Id).LongCount();
    }
    private static long Count(ObservedResource row, string key) => int.TryParse(row.Details.GetValueOrDefault(key),
        NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? Math.Max(0, count) : 0;
}
