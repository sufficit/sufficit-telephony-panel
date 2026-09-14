using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class AcdBoardAccess(PanelAccess access, AcdBoardReader reader)
{
    public bool Configured => reader.Configured;
    public async Task<AcdBoardSample[]> Read(Guid? context, CancellationToken ct)
    {
        var permissions = await access.PermissionsAsync(ct);
        context = permissions.SelectContext(context);
        var result = await reader.Read(context, ct);
        permissions = await access.PermissionsAsync(ct);
        permissions.SelectContext(context);
        return result.Where(s => permissions.CanRead(s.ContextId)).ToArray();
    }
}
