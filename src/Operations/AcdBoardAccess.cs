using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class AcdBoardAccess(PanelAccess access, AcdBoardReader reader)
{
    public bool Configured => reader.Configured;
    public async Task<AcdBoardSample[]> Read(Guid? context, CancellationToken ct)
    {
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException();
        var result = await reader.Read(context, ct);
        if (!await access.AllowedAsync(ct)) throw new UnauthorizedAccessException();
        return result;
    }
}
