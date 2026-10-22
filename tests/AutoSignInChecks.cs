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
        check(login == "/login?returnUrl=%2Fqueues%3Fcontextid%3D2b82363a32334b6ab5caf70be3e12006%26queueid%3D0b48", "Return address keeps the filters");
        check(PanelReturnUrl.Validate("/queues?contextid=x&queueid=y", "") == "/queues?contextid=x&queueid=y" && PanelReturnUrl.Validate("/infrastructure", "") == "/infrastructure", "Return URL allowlist covers queues and infrastructure");
    }
}
