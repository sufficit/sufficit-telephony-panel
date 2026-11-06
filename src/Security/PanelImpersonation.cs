using System.Net.Http.Headers;
using System.Text.Json;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>
/// "Entrar como" (owner, 2026-11-06): a manager opens the panel as a real customer person to find real failures.
/// Identity exchanges the manager's own access token for a 30-minute token of the person (token exchange with
/// <c>impersonated_subject</c>; sub = person, act = manager), so the panel resolves the person's real grants.
/// The panel never offers listen or whisper in that session and always shows who is acting.
/// </summary>
public sealed class PanelImpersonation(IHttpClientFactory clients, IConfiguration configuration, TimeProvider clock, ILogger<PanelImpersonation> log)
{
    public const string ActorClaim = "panel_actor";
    public const string ActorNameClaim = "panel_actor_name";
    public const string ReturnSessionClaim = "panel_return_session";
    public const string ContextClaim = "panel_impersonated_context";

    private string Authority => (configuration["Panel:Impersonation:Authority"] is { Length: > 0 } a ? a : "https://identity.sufficit.com.br").TrimEnd('/');
    private string ClientId => configuration["Panel:Impersonation:ClientId"] is { Length: > 0 } id ? id : "sufficit-telephony-panel";

    public sealed record Issued(string AccessToken, DateTimeOffset ExpiresAt, string? Name);

    /// <summary>The person's token, or the Identity refusal reason ("access_denied: ...") when refused.</summary>
    public async Task<(Issued? Token, string? Refusal)> ExchangeAsync(string managerAccessToken, string targetUserId, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = ClientId,
            ["subject_token"] = managerAccessToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["impersonated_subject"] = targetUserId,
            ["scope"] = "openid profile roles entitlements",
        };
        if (configuration["Identity:ClientSecret"] is { Length: > 0 } secret) form["client_secret"] = secret;
        try
        {
            var http = clients.CreateClient("panel-impersonation");
            using var response = await http.PostAsync(Authority + "/connect/token", new FormUrlEncodedContent(form), ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;
            string? Read(JsonElement element, string name) => element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            if (!response.IsSuccessStatusCode || Read(root, "access_token") is not { Length: > 0 } access)
            {
                var refusal = $"{Read(root, "error") ?? ((int)response.StatusCode).ToString()}: {Read(root, "error_description")}";
                log.LogWarning("Impersonation refused by Identity: {Refusal}", refusal);
                return (null, refusal);
            }
            var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? Math.Clamp(s, 60, 3600) : 1800;
            string? name = null;
            using (var request = new HttpRequestMessage(HttpMethod.Get, Authority + "/connect/userinfo"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                using var info = await http.SendAsync(request, ct);
                if (info.IsSuccessStatusCode)
                {
                    using var body = JsonDocument.Parse(await info.Content.ReadAsStringAsync(ct));
                    name = Read(body.RootElement, "name") ?? Read(body.RootElement, "email") ?? Read(body.RootElement, "preferred_username");
                }
            }
            return (new Issued(access, clock.GetUtcNow().AddSeconds(expiresIn), name), null);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning("Impersonation request failed ({ErrorType})", error.GetType().Name);
            return (null, "unavailable: Identity did not answer.");
        }
    }

    public static bool IsImpersonating(System.Security.Claims.ClaimsPrincipal user) => user.FindFirst(ActorClaim) is not null;
}
