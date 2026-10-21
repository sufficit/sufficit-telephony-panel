using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Context handed to the Hermes web chat so support can act without asking.</summary>
/// <remarks>Built only on the server from validated identity and current permissions; never contains tokens or call data.</remarks>
public sealed record SupportRequest(string Reason, string Subject, string Name, string Email, string Context,
    string Permissions, string Request, string Reference);

/// <summary>Hermes (Chatwoot) web widget settings plus the identity proof for the support contact.</summary>
public sealed class SupportChat
{
    public const string DeniedCookie = "__Secure-Sufficit.Telephony.Panel.Support";
    private const string Purpose = "panel-support-denied-v1";
    private static readonly TimeSpan DeniedLifetime = TimeSpan.FromMinutes(10);
    private static readonly Regex ContextGrant = new("^[a-z0-9_.-]+:([0-9a-fA-F-]{32,36})$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10));
    private readonly byte[]? secret;
    private readonly IDataProtector protector;

    public SupportChat(IConfiguration configuration, IDataProtectionProvider protection)
    {
        BaseUrl = (configuration["Support:BaseUrl"] ?? "https://chatwoot.sufficit.com.br").TrimEnd('/');
        WebsiteToken = configuration["Support:WebsiteToken"] ?? "";
        // The widget HMAC secret stays on the server; only the per-user hash reaches the browser.
        if (configuration["Support:HmacSecretFile"] is { Length: > 0 } file && File.Exists(file))
            secret = Encoding.UTF8.GetBytes(File.ReadAllText(file).Trim());
        protector = protection.CreateProtector(Purpose);
    }

    public string BaseUrl { get; }
    public string WebsiteToken { get; }
    /// <summary>Without the HMAC secret the contact could be impersonated, so the chat stays off.</summary>
    public bool Enabled => WebsiteToken.Length > 0 && secret is { Length: > 0 };
    public static string Release { get; } = typeof(SupportChat).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    /// <summary>Chatwoot identity validation: hex HMAC-SHA256 of the contact identifier with the inbox secret.</summary>
    public string IdentifierHash(string identifier) =>
        secret is null ? "" : Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(identifier)));

    public static string Describe(CurrentAuthorization permissions) =>
        $"manager={(permissions.Manager ? "sim" : "não")}; telephony.panel=[{string.Join(", ", permissions.PanelContexts.Order().Select(c => c.ToString("N")))}]; " +
        $"telephony.monitor=[{string.Join(", ", permissions.MonitorContexts.Order().Select(c => c.ToString("N")))}]";

    public static string Reference(string path) => $"telephony-panel {Release} · {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC · {path}";

    /// <summary>Company contexts the person already holds in other Sufficit grants, as a hint for support.</summary>
    public static string[] KnownContexts(ClaimsPrincipal? principal) => principal?.Claims
        .Where(c => c.Type is "entitlements" or "directive")
        .Select(c => ContextGrant.Match(c.Value))
        .Where(m => m.Success && Guid.TryParse(m.Groups[1].Value, out var id) && id != Guid.Empty)
        .Select(m => Guid.Parse(m.Groups[1].Value).ToString("N"))
        .Distinct().Order().ToArray() ?? [];

    /// <summary>Request for a person Identity authenticated but the panel denied (no session exists yet).</summary>
    public static SupportRequest Denied(ClaimsPrincipal? principal, CurrentAuthorization permissions)
    {
        var contexts = KnownContexts(principal);
        return new("login-denied", principal?.FindFirst("sub")?.Value ?? "", DisplayName(principal), principal?.FindFirst("email")?.Value ?? "",
            contexts.Length == 0 ? "não identificado" : string.Join(", ", contexts), Describe(permissions),
            contexts.Length == 0 ? "telephony.panel:<contexto da empresa>" : string.Join(", ", contexts.Select(c => "telephony.panel:" + c)),
            Reference("/login"));
    }

    public static string DisplayName(ClaimsPrincipal? principal) =>
        principal?.FindFirst("name")?.Value ?? principal?.FindFirst("preferred_username")?.Value ?? principal?.FindFirst("email")?.Value ?? "";

    public string ProtectDenied(SupportRequest request) => protector.ToTimeLimitedDataProtector()
        .Protect(JsonSerializer.Serialize(request), DeniedLifetime);

    public SupportRequest? UnprotectDenied(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 8192) return null;
        try { return JsonSerializer.Deserialize<SupportRequest>(protector.ToTimeLimitedDataProtector().Unprotect(value)); }
        catch (Exception e) when (e is CryptographicException or JsonException or FormatException) { return null; }
    }

    /// <summary>Everything the browser needs to open the widget; the HMAC secret is never included.</summary>
    public object Payload(SupportRequest request, string locale, bool dark) => new
    {
        baseUrl = BaseUrl,
        websiteToken = WebsiteToken,
        locale = locale == "pt-BR" ? "pt_BR" : "en",
        darkMode = dark ? "dark" : "light",
        user = request.Subject.Length == 0 ? null : new
        {
            identifier = request.Subject,
            name = request.Name.Length > 0 ? request.Name : request.Email,
            email = request.Email,
            identifier_hash = IdentifierHash(request.Subject)
        },
        attributes = new Dictionary<string, string>
        {
            ["painel_motivo"] = request.Reason,
            ["painel_usuario"] = request.Subject,
            ["painel_contexto"] = request.Context,
            ["painel_permissoes"] = request.Permissions,
            ["painel_pedido"] = request.Request,
            ["painel_referencia"] = request.Reference
        }
    };
}
