namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Private, administrator-owned source mapping. Never supplied by browser parameters.</summary>
public sealed record AcdBoardSource(string Node, Guid NodeId, Guid[] ContextIds, string BaseUrl, string KeyFile);
