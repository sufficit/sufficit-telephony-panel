namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Header figures of the operation board (owner request 2026-10-31), over the tiles that pass the filters:
/// extensions "active calls / reachable / cards", queues "waiting / queues", trunks "active calls / trunks".
/// A call counts once per leg seen on a card, so a call between two extensions counts on both.
/// </summary>
public static class BoardTotals
{
    /// <summary>
    /// Distinct call channels on the cards, plus one for each card in use or ringing whose call has no channel on the
    /// board (the device state says it is busy before or without the channel events).
    /// </summary>
    public static int ActiveCalls(IEnumerable<OperatorTile> tiles)
    {
        var list = tiles as IReadOnlyCollection<OperatorTile> ?? tiles.ToArray();
        return list.SelectMany(t => t.Channels).Select(c => c.Id).Distinct(StringComparer.Ordinal).Count()
            + list.Count(t => t.Channels.Length == 0 && t.Status.Tone is "busy" or "ringing");
    }

    /// <summary>Cards whose current state says the device answers: reachable, registered, in use or ringing.</summary>
    public static int Reachable(IEnumerable<OperatorTile> tiles)
        => tiles.Count(t => t.Status.Tone is "online" or "busy" or "ringing");

    /// <summary>Calls waiting: observed waiting visits, or the pending count of a native QUEUE queue.</summary>
    public static int Waiting(IEnumerable<OperatorTile> tiles)
        => tiles.Sum(t => t.QueueCalls.Length > 0 ? t.QueueCalls.Length
            : t.Queue is { State: "current" } queue && int.TryParse(queue.Details.GetValueOrDefault("pending"), out var pending) && pending > 0 ? pending : 0);
}
