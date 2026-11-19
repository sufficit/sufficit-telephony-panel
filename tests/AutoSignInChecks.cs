using Microsoft.AspNetCore.Http;
using Sufficit.Telephony.Panel.Security;

internal static class AutoSignInChecks
{
    public static void Run(Action<bool, string> check)
    {
        HttpRequest Request(string path, string query = "", string accept = "text/html,application/xhtml+xml", string method = "GET")
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method; context.Request.Path = path; context.Request.QueryString = new QueryString(query);
            context.Request.Headers.Accept = accept;
            return context.Request;
        }
        var pending=Request("/");pending.Headers.Cookie=AutoSignIn.AttemptCookie+"=1";
        check(!AutoSignIn.CompletedAttempt(pending), "A pending first access is not a completed failed login");
        var speculative=Request("/");speculative.Headers["Sec-Purpose"]="prefetch;prerender";
        check(!AutoSignIn.Applies(speculative,false), "Combined browser prerender/prefetch cannot start authentication");
        check(AutoSignIn.Applies(Request("/queues", "?contextid=abc&queueid=def"), false), "Anonymous deep link signs in automatically");
        check(AutoSignIn.Applies(Request("/"), false), "Anonymous landing signs in automatically");
        check(!AutoSignIn.Applies(Request("/queues"), true), "Signed-in users are not redirected");
        check(!AutoSignIn.Applies(Request("/", "?login=failed"), false) && !AutoSignIn.Applies(Request("/", "?login=denied"), false), "Sign-in outcomes stay visible (no loop)");
        check(!AutoSignIn.Applies(Request("/", "?signedout=1"), false), "Page after logout stays visible");
        check(!AutoSignIn.Applies(Request("/", "?preview=true"), false), "Preview fixtures are not redirected");
        check(!AutoSignIn.Applies(Request("/api/fleet"), false) && !AutoSignIn.Applies(Request("/_blazor"), false), "APIs and transport are not redirected");
        check(!AutoSignIn.Applies(Request("/queues", "", "application/json"), false), "Non-document requests are not redirected");
        check(!AutoSignIn.Applies(Request("/queues", "", method: "POST"), false), "Only GET navigations");
        var login = AutoSignIn.LoginPath(Request("/queues", "?contextid=2b82363a32334b6ab5caf70be3e12006&queueid=0b48"), "");
        check(login == "/login?returnUrl=%2Fqueues%3Fcontextid%3D2b82363a32334b6ab5caf70be3e12006%26queueid%3D0b48&automatic=1", "Return address keeps the filters");
        var complete=Request("/");complete.Headers.Cookie=AutoSignIn.AttemptCookie+"=completed";
        check(AutoSignIn.CompletedAttempt(complete), "Only completed automatic login can stop a genuine redirect loop");
        var legacy=Request("/");legacy.Headers.Cookie=AutoSignIn.LegacyAttemptCookie+"=1";
        check(!AutoSignIn.CompletedAttempt(legacy), "Old pending cookie does not block a new day's sign-in");
        var xhr=Request("/");xhr.Headers["Sec-Fetch-Dest"]="empty";
        check(!AutoSignIn.Applies(xhr,false), "Document-accepting XHR cannot start sign-in");
        var manual=new DefaultHttpContext();AutoSignIn.TicketReceived(manual,AutoSignIn.Challenge("/board",false));
        check(manual.Response.Headers.SetCookie.Count==0, "Manual sign-in does not create an automatic-loop marker");
        var automatic=new DefaultHttpContext();AutoSignIn.TicketReceived(automatic,AutoSignIn.Challenge("/board?contextid=x",true));
        var cookie=automatic.Response.Headers.SetCookie.ToString();
        check(cookie.Contains(AutoSignIn.AttemptCookie+"=completed") && cookie.Contains("secure") && cookie.Contains("httponly")
            && cookie.Contains("path=/") && !cookie.Contains("domain="), "Completion marker is host-only, protected from script and short lived");
        var protection=new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider().CreateProtector("signin-state-test");
        var format=new Microsoft.AspNetCore.Authentication.PropertiesDataFormat(protection);
        var restored=format.Unprotect(format.Protect(AutoSignIn.Challenge("/board?contextid=x",true)));
        var returned=new DefaultHttpContext();AutoSignIn.TicketReceived(returned,restored);
        check(returned.Response.Headers.SetCookie.ToString().Contains(AutoSignIn.AttemptCookie+"=completed"),
            "The automatic flag survives protected OIDC state before ticket completion");
        check(automatic.Response.StatusCode==200 && automatic.User.Identity?.IsAuthenticated!=true, "Loop marker never authenticates or grants access");
        check(AutoSignIn.Challenge("/board?contextid=x",true).RedirectUri=="/board?contextid=x", "OIDC state preserves the allowed original destination");
        check(PanelReturnUrl.Validate("/queues?contextid=x&queueid=y", "") == "/queues?contextid=x&queueid=y" && PanelReturnUrl.Validate("/infrastructure", "") == "/infrastructure", "Return URL allowlist covers queues and infrastructure");
    }
}
