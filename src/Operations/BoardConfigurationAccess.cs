using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class BoardConfigurationAccess(PanelAccess access, BoardRuleStore store, ILogger<BoardConfigurationAccess> logger)
{
    public async Task<BoardConfiguration> Read(CancellationToken ct = default)
    {
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException("Sua sessão não tem acesso de gerente. Entre novamente.");
        return await store.Read(ct);
    }
    public async Task<BoardConfiguration> Save(BoardConfiguration draft, CancellationToken ct = default)
    {
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException("Sua sessão não tem acesso de gerente. Entre novamente.");
        var saved = await store.Save(draft, ct);
        logger.LogInformation("Board rules saved: revision {Revision}, count {Count}", saved.Revision, saved.Rules.Length);
        return saved;
    }
}
