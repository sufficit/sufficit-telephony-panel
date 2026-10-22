namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Private, administrator-owned source mapping. Never supplied by browser parameters.</summary>
/// <param name="AnyContext">The controller serves every company: besides <paramref name="ContextIds"/> (read in the
/// overview), any context the user is authorized for is read on demand when that context is selected.</param>
public sealed record QueueBoardSource(string Node, Guid NodeId, Guid[] ContextIds, string BaseUrl, string KeyFile, bool AnyContext = false);
