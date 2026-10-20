using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class QueueBoardAccess(PanelAccess access, QueueBoardReader reader, QueueDiagnosticsReader? diagnostics = null)
{
    public bool Configured => reader.Configured;
    public async Task<T> Diagnostic<T>(Guid context, Guid node, string endpoint, CancellationToken ct) where T : class
    {
        var permissions = await access.PermissionsAsync(ct);
        if (!permissions.CanRead(context)) throw new UnauthorizedAccessException("Context is not authorized.");
        if (diagnostics is null) throw new InvalidOperationException("Call diagnostics are not configured.");
        var result = await diagnostics.Read<T>(context, node, endpoint, ct);
        permissions = await access.PermissionsAsync(ct);
        if (!permissions.CanRead(context)) throw new UnauthorizedAccessException("Context access expired.");
        return result;
    }
    public async Task<QueueBoardSample[]> Read(Guid? context, CancellationToken ct)
    {
        var permissions = await access.PermissionsAsync(ct);
        context = permissions.SelectContext(context);
        var result = await reader.Read(context, ct);
        permissions = await access.PermissionsAsync(ct);
        permissions.SelectContext(context);
        return result.Where(s => permissions.CanRead(s.ContextId)).ToArray();
    }
}
