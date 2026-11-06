using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>
/// TV pairing (owner, 2026-11-06): the TV opens /tv and shows a short code; Sufficit staff type it on the customer's
/// panel access screen, which creates an Identity system user holding only <c>telephony.panel:&lt;customer&gt;</c> and an
/// enrollment code for the TV client, and posts both here. The panel exchanges the enrollment code for a refresh
/// token and the waiting TV signs in. Possession of a valid enrollment code is the authorization; a TV session never
/// carries monitor or global access.
/// </summary>
public sealed class TvPairing(IHttpClientFactory clients, PanelSessions sessions, IConfiguration configuration, TimeProvider clock, ILogger<TvPairing> log)
{
    public const string Cookie = "__Secure-Sufficit.Telephony.Panel.Tv";
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(365);
    private const string Alphabet = "ACDEFHJKMNPRTWXY3479"; // no look-alikes (0/O, 1/I/L, 2/Z, 5/S, 8/B)
    private const int MaxPending = 500, MaxAttemptsPerCode = 3;
    public const string EnrollmentGrant = "urn:sufficit:params:oauth:grant-type:system-user-enrollment";

    private readonly ConcurrentDictionary<string, Pending> byCode = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Pending> byId = new(StringComparer.Ordinal);

    public string ClientId => configuration["Panel:Tv:ClientId"] is { Length: > 0 } id ? id : "sufficit-telephony-panel-tv";
    private string Authority => (configuration["Panel:Tv:Authority"] is { Length: > 0 } a ? a : "https://identity.sufficit.com.br").TrimEnd('/');

    public sealed record Paired(string Subject, string Name, Guid Context, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
    private sealed class Pending(string id, string code, DateTimeOffset expires)
    {
        public string Id { get; } = id;
        public string Code { get; } = code;
        public DateTimeOffset Expires { get; } = expires;
        public int Attempts;
        public Paired? Result;
    }

    public enum PairResult { Paired, UnknownCode, InvalidEnrollment, NotTvSafe }

    /// <summary>A new waiting TV: the browser keeps <paramref name="id"/> in a cookie and shows <paramref name="code"/>.</summary>
    public (string Id, string Code) Begin()
    {
        Sweep();
        if (byId.Count >= MaxPending) throw new InvalidOperationException("Too many waiting TVs.");
        for (var i = 0; i < 10; i++)
        {
            var code = string.Concat(Enumerable.Range(0, 8).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]));
            var pending = new Pending(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), code, clock.GetUtcNow() + CodeLifetime);
            if (!byCode.TryAdd(code, pending)) continue;
            byId[pending.Id] = pending;
            return (pending.Id, code);
        }
        throw new InvalidOperationException("Could not allocate a pairing code.");
    }

    public static string? NormalizeCode(string? code)
    {
        var value = new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return value.Length == 8 && value.All(Alphabet.Contains) ? value : null;
    }

    public static string Display(string code) => code[..4] + "-" + code[4..];

    /// <summary>Waiting, paired or gone (expired or unknown).</summary>
    public string Status(string? id)
    {
        if (id is null || !byId.TryGetValue(id, out var pending) || pending.Expires <= clock.GetUtcNow()) return "expired";
        return pending.Result is null ? "waiting" : "paired";
    }

    /// <summary>Hands the paired session to the TV once; the pairing is gone afterwards.</summary>
    public Paired? Take(string? id)
    {
        if (id is null || !byId.TryGetValue(id, out var pending) || pending.Result is not { } result) return null;
        Drop(pending);
        return result;
    }

    public async Task<PairResult> PairAsync(string? code, string? enrollmentCode, CancellationToken ct)
    {
        Sweep();
        var normalized = NormalizeCode(code);
        if (normalized is null || !byCode.TryGetValue(normalized, out var pending) || pending.Result is not null) return PairResult.UnknownCode;
        if (Interlocked.Increment(ref pending.Attempts) > MaxAttemptsPerCode) { Drop(pending); return PairResult.UnknownCode; }
        if (string.IsNullOrWhiteSpace(enrollmentCode) || enrollmentCode.Length > 512) return PairResult.InvalidEnrollment;

        var tokens = await TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = EnrollmentGrant, ["enrollment_code"] = enrollmentCode.Trim(), ["client_id"] = ClientId,
            ["scope"] = "openid profile entitlements",
        }, ct);
        if (tokens is null) return PairResult.InvalidEnrollment;
        var (subject, name) = await UserInfoAsync(tokens.Value.Access, ct);
        if (subject is null) return PairResult.InvalidEnrollment;
        var permissions = await sessions.PermissionsAsync(subject, tokens.Value.Access, ct);
        if (!IsTvSafe(permissions)) { log.LogWarning("TV pairing refused: subject {Subject} is not limited to one panel context", subject); return PairResult.NotTvSafe; }
        pending.Result = new Paired(subject, name ?? "TV", permissions.PanelContexts.Single(), tokens.Value.Access, tokens.Value.Refresh,
            clock.GetUtcNow().AddSeconds(tokens.Value.ExpiresIn));
        log.LogWarning("TV paired: subject {Subject} context {Context}", subject, pending.Result.Context.ToString("N"));
        return PairResult.Paired;
    }

    /// <summary>A TV only views one customer: no manager role, no monitor grant, no all-contexts grant.</summary>
    public static bool IsTvSafe(CurrentAuthorization permissions)
        => !permissions.Manager && permissions.MonitorContexts.Count == 0 && permissions.PanelContexts.Count == 1
            && !permissions.PanelContexts.Contains(Guid.Empty);

    /// <summary>Refreshes the access token of a TV session; null when the credential was revoked or expired.</summary>
    public async Task<(string Access, string Refresh, DateTimeOffset ExpiresAt)?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var tokens = await TokenAsync(new Dictionary<string, string>
        { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken, ["client_id"] = ClientId }, ct);
        return tokens is { } t ? (t.Access, t.Refresh, clock.GetUtcNow().AddSeconds(t.ExpiresIn)) : null;
    }

    private async Task<(string Access, string Refresh, int ExpiresIn)?> TokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        try
        {
            using var response = await clients.CreateClient("panel-tv").PostAsync(Authority + "/connect/token", new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning("TV token request ({Grant}) refused: {Status}", form["grant_type"] == EnrollmentGrant ? "enrollment" : "refresh", (int)response.StatusCode);
                return null;
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;
            var access = root.TryGetProperty("access_token", out var a) ? a.GetString() : null;
            var refresh = root.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
            var expires = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? seconds : 3600;
            // A rotated refresh token replaces the old one; keep the old one only when the server does not rotate.
            refresh ??= form.TryGetValue("refresh_token", out var previous) ? previous : null;
            return string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh) ? null : (access, refresh, Math.Clamp(expires, 60, 86400));
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning("TV token request failed ({ErrorType})", error.GetType().Name);
            return null;
        }
    }

    private async Task<(string? Subject, string? Name)> UserInfoAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Authority + "/connect/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await clients.CreateClient("panel-tv").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return (null, null);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;
            string? Read(string property) => root.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            return (Read("sub"), Read("name") ?? Read("preferred_username"));
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or TaskCanceledException) { return (null, null); }
    }

    private void Drop(Pending pending) { byCode.TryRemove(pending.Code, out _); byId.TryRemove(pending.Id, out _); }

    private void Sweep()
    {
        var now = clock.GetUtcNow();
        foreach (var pending in byId.Values.Where(p => p.Expires <= now).ToArray()) Drop(pending);
    }
}
