namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdBoardSample(string Node, Guid ContextId, AcdBoardSnapshot? Snapshot, bool Available);
