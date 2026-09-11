using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class BoardInventoryAccess(PanelAccess access, BoardInventoryStore store)
{
    public async Task<OperatorTile[]> Observe(Guid? context, OperatorTile[] current,
        EventsPanelCardInfo[] cards, bool retain, CancellationToken ct = default)
    {
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException();
        return await store.Observe(context, current, cards, retain, ct);
    }
    public async Task Clear(Guid? context, CancellationToken ct = default)
    {
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException();
        await store.Clear(context, ct);
    }
}
