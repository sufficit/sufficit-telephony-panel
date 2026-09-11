using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Bounded server-only ticket/token cache; browser cookies contain only a random handle.</summary>
public sealed class PanelSessions(IHttpClientFactory clients, TimeProvider clock) : ITicketStore, IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 200 });
    private readonly SemaphoreSlim gate = new(1, 1);
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

    public bool HasTelephoneScope(System.Security.Claims.ClaimsPrincipal principal)
    {
        var key = principal.FindFirstValue("panel_session");
        return key is not null && cache.Get<SessionEntry>(key) is { } entry
            && entry.Ticket.Properties.Items.TryGetValue("panel_entitlements", out var enabled) && enabled == "true";
    }

    public async Task<string?> GetAccessTokenAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!await IsManagerAsync(principal, ct)) return null;
        var key = principal.FindFirstValue("panel_session");
        return key is null ? null : cache.Get<SessionEntry>(key)?.Ticket.Properties.GetTokenValue("access_token");
    }

    public async Task<bool> IsManagerAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var key = principal.FindFirstValue("panel_session");
        if (principal.Identity?.IsAuthenticated != true || key is null) return false;
        await gate.WaitAsync(ct);
        try
        {
            var entry = cache.Get<SessionEntry>(key);
            if (entry is null) return false;
            var token = entry.Ticket.Properties.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(token) || !DateTimeOffset.TryParse(entry.Ticket.Properties.GetTokenValue("expires_at"), out var expires)
                || expires <= clock.GetUtcNow()) { cache.Remove(key); return false; }
            // Refresh current Identity roles, never trust indefinitely cached ID-token authorization.
            if (clock.GetUtcNow() - entry.CheckedAt < TimeSpan.FromSeconds(60)) return entry.Manager;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://identity.sufficit.com.br/connect/userinfo");
                request.Headers.Authorization = new("Bearer", token);
                using var response = await clients.CreateClient("identity").SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var root = json.RootElement;
                entry.Manager = root.GetProperty("sub").GetString() == principal.FindFirstValue("sub")
                    && root.TryGetProperty("role", out var role)
                    && (role.ValueKind == JsonValueKind.Array ? role.EnumerateArray().Any(v => v.GetString() == "manager") : role.GetString() == "manager");
                entry.CheckedAt = clock.GetUtcNow();
                if (!entry.Manager) cache.Remove(key);
                return entry.Manager;
            }
            catch (Exception error) when (error is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException or KeyNotFoundException)
            { entry.Manager = false; return false; }
        }
        finally { gate.Release(); }
    }
    public void Dispose() { cache.Dispose(); gate.Dispose(); }
}
