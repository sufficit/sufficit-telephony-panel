using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel.Security;

static class DisplayPairingChecks
{
    // Identity stand-in: token endpoint, userinfo and current authorization by path.
    private sealed class Identity : HttpMessageHandler, IHttpClientFactory
    {
        public bool EnrollmentAccepted = true;
        public string[] Grants = [];
        public int TokenCalls;
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/connect/token"))
            {
                Interlocked.Increment(ref TokenCalls);
                var form = await request.Content!.ReadAsStringAsync(ct);
                if (form.Contains("system-user-enrollment") && !EnrollmentAccepted) return new HttpResponseMessage(HttpStatusCode.BadRequest);
                return Json(new { access_token = "access-" + TokenCalls, refresh_token = "refresh-" + TokenCalls, expires_in = 3600 });
            }
            if (path == "/telephony/panel/contexts")
                return Json(Grants.Where(g => g.StartsWith("telephony.panel:")).Select(g => Guid.Parse(g.Split(':')[1])).ToArray());
            if (path.StartsWith("/connect/userinfo?entitlements=current"))
                return Json(new { sub = "display-user", authorization = new { schemaVersion = 1, grants = Grants } });
            if (path.StartsWith("/connect/userinfo")) return Json(new { sub = "display-user", name = "Tela da recepção" });
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body)) };
    }

    public static async Task Run(Action<bool, string> check)
    {
        var context = Guid.NewGuid();
        check(DisplayPairing.NormalizeCode("acde-fh3k") == "ACDEFH3K" && DisplayPairing.NormalizeCode("ACDE-FH0K") is null
            && DisplayPairing.NormalizeCode("ACD") is null, "Pairing code accepts its alphabet only, dash and case ignored");
        check(DisplayPairing.Display("ACDEFH3K") == "ACDE-FH3K", "Pairing code is shown in two groups");

        CurrentAuthorization Auth(bool manager, Guid[] panel, Guid[] monitor) => new(manager, [.. panel], [.. monitor]);
        check(DisplayPairing.IsDisplaySafe(Auth(false, [context], [])), "One panel context is display safe");
        check(!DisplayPairing.IsDisplaySafe(Auth(false, [context], [context])), "Monitor grant is never display safe");
        check(!DisplayPairing.IsDisplaySafe(Auth(true, [], [])), "Manager is never display safe");
        check(!DisplayPairing.IsDisplaySafe(Auth(false, [Guid.Empty], [])) && !DisplayPairing.IsDisplaySafe(Auth(false, [context, Guid.NewGuid()], [])),
            "All-contexts or several contexts are not display safe");

        var identity = new Identity { Grants = ["telephony.panel:" + context.ToString("N")] };
        var clock = new FakeClock();
        using var sessions = new PanelSessions(identity, clock);
        var display = new DisplayPairing(identity, sessions, new ConfigurationBuilder().Build(), clock, NullLogger<DisplayPairing>.Instance);

        var (id, code) = display.Begin();
        check(display.Status(id) == "waiting" && display.Take(id) is null, "A new display waits and has nothing to take");
        check(await display.PairAsync("WWWW-WWWW", "sys_enroll_x", default) == DisplayPairing.PairResult.UnknownCode, "Unknown code is refused");
        var paired = await display.PairAsync(DisplayPairing.Display(code).ToLowerInvariant(), "sys_enroll_valid", default);
        check(paired == DisplayPairing.PairResult.Paired && display.Status(id) == "paired", "Valid enrollment pairs the waiting display");
        var session = display.Take(id);
        check(session is { Subject: "display-user", RefreshToken: "refresh-1" } && session.Context == context, "Paired display receives its single context and refresh token");
        check(display.Take(id) is null && display.Status(id) == "expired", "A pairing is taken only once");
        check(await display.PairAsync(code, "sys_enroll_valid", default) == DisplayPairing.PairResult.UnknownCode, "A used code cannot pair again");

        identity.Grants = ["telephony.panel:" + context.ToString("N"), "telephony.monitor:" + context.ToString("N")];
        var unsafeDisplay = display.Begin();
        check(await display.PairAsync(unsafeDisplay.Code, "sys_enroll_valid", default) == DisplayPairing.PairResult.NotDisplaySafe
            && display.Status(unsafeDisplay.Id) == "waiting", "A credential with monitor access never signs a display in");

        identity.EnrollmentAccepted = false;
        var guessed = display.Begin();
        for (var i = 0; i < 3; i++) await display.PairAsync(guessed.Code, "sys_enroll_bad", default);
        check(await display.PairAsync(guessed.Code, "sys_enroll_bad", default) == DisplayPairing.PairResult.UnknownCode
            && display.Status(guessed.Id) == "expired", "Three failed enrollments burn the code");

        var stale = display.Begin();
        clock.Now += DisplayPairing.CodeLifetime + TimeSpan.FromSeconds(1);
        check(display.Status(stale.Id) == "expired", "Codes expire");

        // Session renewal: an expired display access token is refreshed instead of ending the session.
        identity.Grants = ["telephony.panel:" + context.ToString("N")];
        identity.EnrollmentAccepted = true;
        sessions.Refresher = display.RefreshAsync;
        var properties = new AuthenticationProperties { ExpiresUtc = clock.GetUtcNow().AddDays(365), IsPersistent = true };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "old" },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh-old" },
            new AuthenticationToken { Name = "expires_at", Value = clock.GetUtcNow().AddMinutes(-1).ToString("o") }]);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "display-user")], "display")), properties, "panel");
        var key = await sessions.StoreAsync(ticket);
        var permissions = await sessions.AuthorizationAsync(ticket.Principal, default);
        var renewed = await sessions.RetrieveAsync(key);
        check(permissions.CanRead(context) && renewed?.Properties.GetTokenValue("access_token")?.StartsWith("access-") == true
            && renewed.Properties.GetTokenValue("refresh_token")?.StartsWith("refresh-") == true, "Expired display token is renewed with the refresh token");

        sessions.Refresher = null;
        var person = new AuthenticationProperties { ExpiresUtc = clock.GetUtcNow().AddHours(8) };
        person.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "person" },
            new AuthenticationToken { Name = "expires_at", Value = clock.GetUtcNow().AddMinutes(-1).ToString("o") }]);
        var personTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "display-user")], "test")), person, "panel");
        await sessions.StoreAsync(personTicket);
        check(!(await sessions.AuthorizationAsync(personTicket.Principal, default)).PanelAllowed, "A person session without refresh token still ends with its token");
    }
}
