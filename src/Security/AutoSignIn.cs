using Microsoft.AspNetCore.Http;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>
/// Page navigations by anonymous browsers start the Identity sign-in automatically instead of stopping at
/// the landing page. When Identity already has a session the round trip is silent, so links from the portal
/// (e.g. "Monitorar" in the entry plan) open the panel directly. Sign-in outcomes (?login=...), the page
/// after logout (?signedout=1) and preview fixtures stay visible, which also prevents redirect loops.
/// </summary>
public static class AutoSignIn
{
    private static readonly string[] Pages = ["/", "/board", "/queues", "/infrastructure", "/mesa"];

    public static bool Applies(HttpRequest request, bool authenticated)
    {
        if (authenticated || !HttpMethods.IsGet(request.Method)) return false;
        var path = request.Path.Value is { Length: > 0 } value ? value.TrimEnd('/') : "";
        if (!Pages.Contains(path.Length == 0 ? "/" : path)) return false;
        if (request.Query.ContainsKey("login") || request.Query.ContainsKey("signedout") || request.Query.ContainsKey("preview")) return false;
        // Only real document navigations; never XHR, prefetch or Blazor transport requests.
        return request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Headers["Sec-Purpose"], "prefetch", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Headers["Purpose"], "prefetch", StringComparison.OrdinalIgnoreCase);
    }

    public static string LoginPath(HttpRequest request, string pathBase) =>
        pathBase + "/login?returnUrl=" + Uri.EscapeDataString(pathBase + request.Path + request.QueryString);
}
