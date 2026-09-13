namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdBoardSample(string Node, Guid ContextId, AcdBoardSnapshot? Snapshot, bool Available, Guid NodeId = default)
{
    public bool MatchesNode(string value) => value == "all" || value == Node ||
        Guid.TryParse(value, out var id) && id != Guid.Empty && (NodeId == id || Guid.TryParse(Snapshot?.NodeId, out var snapshotId) && snapshotId == id);
}
