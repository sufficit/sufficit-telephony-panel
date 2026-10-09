using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Identity.Authorization;
namespace Sufficit.Telephony.Panel.Security;
public sealed class PanelAccess(AuthenticationStateProvider authentication, PanelSessions sessions)
{
    public async Task<bool> AllowedAsync(CancellationToken ct) =>
        (await PermissionsAsync(ct)).PanelAllowed;
    public async Task<CurrentAuthorization> PermissionsAsync(CancellationToken ct) =>
        await sessions.AuthorizationAsync((await authentication.GetAuthenticationStateAsync()).User, ct);
    public async Task<bool> IsManagerAsync(CancellationToken ct) => (await PermissionsAsync(ct)).Manager;
}
