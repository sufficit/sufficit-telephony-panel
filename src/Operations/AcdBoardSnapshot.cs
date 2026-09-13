namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdBoardSnapshot(string ContextId, string NodeId, DateTimeOffset ObservedAt,
    bool ControllerConnected, bool ObserverOnly, AcdBoardQueue[] Queues, AcdBoardVisit[]? Visits = null,
    AcdAgentPresence[]? Agents = null)
{
    public bool Fresh(DateTimeOffset now) => ControllerConnected && !ObserverOnly &&
        ObservedAt <= now.AddSeconds(5) && ObservedAt >= now.AddSeconds(-20);
}
