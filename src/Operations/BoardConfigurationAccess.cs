using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class BoardConfigurationAccess(PanelAccess access, BoardRuleStore store, ILogger<BoardConfigurationAccess> logger)
{
    public async Task<BoardConfiguration> Read(CancellationToken ct = default)
    {
        var permissions = await access.PermissionsAsync(ct);
        if (!permissions.PanelAllowed) throw new UnauthorizedAccessException();
        if (!permissions.Manager) return new BoardConfiguration();
        return await store.Read(ct);
    }
    public async Task<BoardConfiguration> Save(BoardConfiguration draft, CancellationToken ct = default)
    {
        if (!await access.IsManagerAsync(ct)) throw new UnauthorizedAccessException("Manager access is required to edit shared board rules.");
        var saved = await store.Save(draft, ct);
        logger.LogInformation("Board rules saved: revision {Revision}, count {Count}", saved.Revision, saved.Rules.Length);
        return saved;
    }
}
