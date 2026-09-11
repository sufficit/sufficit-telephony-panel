namespace Sufficit.Telephony.Panel.Operations;

public sealed record BoardInventoryState
{
    public int Schema { get; init; } = 1;
    public KnownBoardResource[] Resources { get; init; } = [];
    public Dictionary<string, DateTimeOffset> ClearedAt { get; init; } = new();
}
