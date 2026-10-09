using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class BoardInventoryAccess(PanelAccess access, BoardInventoryStore store)
{
    public async Task<OperatorTile[]> Observe(Guid? context, OperatorTile[] current,
        EventsPanelCardInfo[] cards, bool retain, CancellationToken ct = default)
    {
        var permissions = await access.PermissionsAsync(ct);
        context = permissions.SelectContext(context);
        // Contextual sessions must never read or contribute to the manager's
        // central inventory. Multi-context views retain their current authorized cards.
        if (!permissions.Manager && !context.HasValue) return current;
        return await store.Observe(context, current, cards, retain, ct);
    }
    public async Task Clear(Guid? context, CancellationToken ct = default)
    {
        if (!await access.IsManagerAsync(ct)) throw new UnauthorizedAccessException();
        await store.Clear(context, ct);
    }
}
