using Microsoft.AspNetCore.Components.Authorization;
namespace Sufficit.Telephony.Panel.Security;
public sealed class PanelAccess(AuthenticationStateProvider authentication, PanelSessions sessions)
{
    public async Task<bool> AllowedAsync(CancellationToken ct) =>
        await sessions.IsManagerAsync((await authentication.GetAuthenticationStateAsync()).User, ct);
}
