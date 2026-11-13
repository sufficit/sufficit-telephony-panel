namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Private, administrator-owned source mapping. Never supplied by browser parameters.</summary>
/// <param name="AnyContext">The controller serves every company: besides <paramref name="ContextIds"/> (read in the
/// overview), the global view reads all runtime contexts through one private node snapshot. Authorized individual companies and portfolios are read on demand without global discovery.</param>
public sealed record QueueBoardSource(string Node, Guid NodeId, Guid[] ContextIds, string BaseUrl, string KeyFile, bool AnyContext = false,
    string? GlobalBaseUrl = null, string? GlobalKeyFile = null);
