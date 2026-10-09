using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Bounded server-only ticket/token cache; browser cookies contain only a random handle.</summary>
public sealed class PanelSessions(IHttpClientFactory clients, TimeProvider clock) : ITicketStore, IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 200 });
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CurrentAuthorizationClient authorization = new(clients, clock);
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.CreateVersion7().ToString("N");
        ((ClaimsIdentity)ticket.Principal.Identity!).AddClaim(new Claim("panel_session", key));
        await RenewAsync(key, ticket);
        return key;
    }
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        cache.Set(key, new SessionEntry(ticket), new MemoryCacheEntryOptions
        { Size = 1, AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8) });
        return Task.CompletedTask;
    }
    public Task<AuthenticationTicket?> RetrieveAsync(string key) => Task.FromResult(cache.Get<SessionEntry>(key)?.Ticket);
    public Task RemoveAsync(string key) { cache.Remove(key); return Task.CompletedTask; }

    public async Task<string?> GetAccessTokenAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!(await AuthorizationAsync(principal, ct)).PanelAllowed) return null;
        var key = principal.FindFirstValue("panel_session");
        return key is null ? null : cache.Get<SessionEntry>(key)?.Ticket.Properties.GetTokenValue("access_token");
    }

    public async Task<bool> IsManagerAsync(ClaimsPrincipal principal, CancellationToken ct) =>
        (await AuthorizationAsync(principal, ct)).Manager;

    public async Task<CurrentAuthorization> AuthorizationAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var key = principal.FindFirstValue("panel_session");
        if (principal.Identity?.IsAuthenticated != true || key is null) return CurrentAuthorization.None;
        await gate.WaitAsync(ct);
        try
        {
            var entry = cache.Get<SessionEntry>(key);
            if (entry is null) return CurrentAuthorization.None;
            var token = entry.Ticket.Properties.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(token) || !DateTimeOffset.TryParse(entry.Ticket.Properties.GetTokenValue("expires_at"), out var expires)
                || expires <= clock.GetUtcNow()) { cache.Remove(key); return CurrentAuthorization.None; }
            try
            {
                var permissions = await authorization.ReadAsync(principal.FindFirstValue("sub")!, token, ct);
                if (!permissions.PanelAllowed) cache.Remove(key);
                return permissions;
            }
            catch (Exception error) when (error is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
            { return CurrentAuthorization.None; }
        }
        finally { gate.Release(); }
    }
    public void Dispose() { authorization.Dispose(); cache.Dispose(); gate.Dispose(); }
}
