namespace Sufficit.Telephony.Panel.Operations;

public static class QueueCardOrder
{
    public static OperatorTile[] Sort(IEnumerable<OperatorTile> source, bool byUtilization) => source
        .OrderByDescending(t => byUtilization ? BoardTotals.Waiting([t]) : t.HasObservedActivity ? 1 : 0)
        .ThenByDescending(BoardTotals.CurrentQueue).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
        .ThenBy(t => t.Node, StringComparer.Ordinal).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();
}
