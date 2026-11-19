using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>
/// Page navigations by anonymous browsers start the Identity sign-in automatically instead of stopping at
/// the landing page. When Identity already has a session the round trip is silent, so links from the portal
/// (e.g. "Monitorar" in the entry plan) open the panel directly. Sign-in outcomes (?login=...), the page
/// after logout (?signedout=1) and preview fixtures stay visible, which also prevents redirect loops.
/// </summary>
public static class AutoSignIn
{
    // Host-only marker is written only after the automatic OIDC round trip completed.
    // Pending/legacy markers cannot prove that a usable session failed to arrive.
    public const string AttemptCookie = "__Host-Sufficit.Telephony.Panel.SignIn";
    public const string LegacyAttemptCookie = "__Secure-Sufficit.Telephony.Panel.SignIn";
    public const string AutomaticProperty = "panel.automatic-signin";
    public static bool CompletedAttempt(HttpRequest request) => request.Cookies[AttemptCookie] == "completed";

    public static AuthenticationProperties Challenge(string safeReturn, bool automatic)
    {
        var properties = new AuthenticationProperties { RedirectUri = safeReturn };
        if (automatic) properties.Items[AutomaticProperty] = "1";
        return properties;
    }

    public static void TicketReceived(HttpContext context, AuthenticationProperties? properties)
    {
        if (properties is null || !properties.Items.TryGetValue(AutomaticProperty, out var automatic) || automatic != "1") return;
        context.Response.Cookies.Append(AttemptCookie, "completed", new CookieOptions {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromSeconds(30) });
    }

    public static void ClearAttempt(HttpContext context, string pathBase)
    {
        if (context.Request.Cookies.ContainsKey(AttemptCookie))
            context.Response.Cookies.Delete(AttemptCookie, new CookieOptions { Path = "/", Secure = true });
        if (context.Request.Cookies.ContainsKey(LegacyAttemptCookie))
            context.Response.Cookies.Delete(LegacyAttemptCookie, new CookieOptions { Path = pathBase.Length > 0 ? pathBase : "/", Secure = true });
    }

    private static readonly string[] Pages = ["/", "/board", "/queues", "/infrastructure", "/mesa"];

    public static bool Applies(HttpRequest request, bool authenticated)
    {
        if (authenticated || !HttpMethods.IsGet(request.Method)) return false;
        var path = request.Path.Value is { Length: > 0 } value ? value.TrimEnd('/') : "";
        if (!Pages.Contains(path.Length == 0 ? "/" : path)) return false;
        if (request.Query.ContainsKey("login") || request.Query.ContainsKey("signedout") || request.Query.ContainsKey("preview")) return false;
        // Only real document navigations; never XHR, prefetch or Blazor transport requests.
        return request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !Speculative(request.Headers["Sec-Purpose"].ToString())
            && !Speculative(request.Headers["Purpose"].ToString())
            && !Speculative(request.Headers["X-Purpose"].ToString())
            && (request.Headers["Sec-Fetch-Dest"].Count == 0 || request.Headers["Sec-Fetch-Dest"] == "document");
    }

    private static bool Speculative(string value) => value.Contains("prefetch", StringComparison.OrdinalIgnoreCase)
        || value.Contains("prerender", StringComparison.OrdinalIgnoreCase);

    public static string LoginPath(HttpRequest request, string pathBase) =>
        pathBase + "/login?returnUrl=" + Uri.EscapeDataString(pathBase + request.Path + request.QueryString) + "&automatic=1";
}
