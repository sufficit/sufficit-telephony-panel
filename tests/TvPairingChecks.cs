using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel.Security;

static class TvPairingChecks
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
            if (path.StartsWith("/connect/userinfo?entitlements=current"))
                return Json(new { sub = "tv-user", authorization = new { schemaVersion = 1, grants = Grants } });
            if (path.StartsWith("/connect/userinfo")) return Json(new { sub = "tv-user", name = "TV recepção" });
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body)) };
    }

    public static async Task Run(Action<bool, string> check)
    {
        var context = Guid.NewGuid();
        check(TvPairing.NormalizeCode("acde-fh3k") == "ACDEFH3K" && TvPairing.NormalizeCode("ACDE-FH0K") is null
            && TvPairing.NormalizeCode("ACD") is null, "Pairing code accepts its alphabet only, dash and case ignored");
        check(TvPairing.Display("ACDEFH3K") == "ACDE-FH3K", "Pairing code is shown in two groups");

        CurrentAuthorization Auth(bool manager, Guid[] panel, Guid[] monitor) => new(manager, [.. panel], [.. monitor]);
        check(TvPairing.IsTvSafe(Auth(false, [context], [])), "One panel context is TV safe");
        check(!TvPairing.IsTvSafe(Auth(false, [context], [context])), "Monitor grant is never TV safe");
        check(!TvPairing.IsTvSafe(Auth(true, [], [])), "Manager is never TV safe");
        check(!TvPairing.IsTvSafe(Auth(false, [Guid.Empty], [])) && !TvPairing.IsTvSafe(Auth(false, [context, Guid.NewGuid()], [])),
            "All-contexts or several contexts are not TV safe");

        var identity = new Identity { Grants = ["telephony.panel:" + context.ToString("N")] };
        var clock = new FakeClock();
        using var sessions = new PanelSessions(identity, clock);
        var tv = new TvPairing(identity, sessions, new ConfigurationBuilder().Build(), clock, NullLogger<TvPairing>.Instance);

        var (id, code) = tv.Begin();
        check(tv.Status(id) == "waiting" && tv.Take(id) is null, "A new TV waits and has nothing to take");
        check(await tv.PairAsync("WWWW-WWWW", "sys_enroll_x", default) == TvPairing.PairResult.UnknownCode, "Unknown code is refused");
        var paired = await tv.PairAsync(TvPairing.Display(code).ToLowerInvariant(), "sys_enroll_valid", default);
        check(paired == TvPairing.PairResult.Paired && tv.Status(id) == "paired", "Valid enrollment pairs the waiting TV");
        var session = tv.Take(id);
        check(session is { Subject: "tv-user", RefreshToken: "refresh-1" } && session.Context == context, "Paired TV receives its single context and refresh token");
        check(tv.Take(id) is null && tv.Status(id) == "expired", "A pairing is taken only once");
        check(await tv.PairAsync(code, "sys_enroll_valid", default) == TvPairing.PairResult.UnknownCode, "A used code cannot pair again");

        identity.Grants = ["telephony.panel:" + context.ToString("N"), "telephony.monitor:" + context.ToString("N")];
        var unsafeTv = tv.Begin();
        check(await tv.PairAsync(unsafeTv.Code, "sys_enroll_valid", default) == TvPairing.PairResult.NotTvSafe
            && tv.Status(unsafeTv.Id) == "waiting", "A credential with monitor access never signs a TV in");

        identity.EnrollmentAccepted = false;
        var guessed = tv.Begin();
        for (var i = 0; i < 3; i++) await tv.PairAsync(guessed.Code, "sys_enroll_bad", default);
        check(await tv.PairAsync(guessed.Code, "sys_enroll_bad", default) == TvPairing.PairResult.UnknownCode
            && tv.Status(guessed.Id) == "expired", "Three failed enrollments burn the code");

        var stale = tv.Begin();
        clock.Now += TvPairing.CodeLifetime + TimeSpan.FromSeconds(1);
        check(tv.Status(stale.Id) == "expired", "Codes expire");

        // Session renewal: an expired TV access token is refreshed instead of ending the session.
        identity.Grants = ["telephony.panel:" + context.ToString("N")];
        identity.EnrollmentAccepted = true;
        sessions.Refresher = tv.RefreshAsync;
        var properties = new AuthenticationProperties { ExpiresUtc = clock.GetUtcNow().AddDays(365), IsPersistent = true };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "old" },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh-old" },
            new AuthenticationToken { Name = "expires_at", Value = clock.GetUtcNow().AddMinutes(-1).ToString("o") }]);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "tv-user")], "tv")), properties, "panel");
        var key = await sessions.StoreAsync(ticket);
        var permissions = await sessions.AuthorizationAsync(ticket.Principal, default);
        var renewed = await sessions.RetrieveAsync(key);
        check(permissions.CanRead(context) && renewed?.Properties.GetTokenValue("access_token")?.StartsWith("access-") == true
            && renewed.Properties.GetTokenValue("refresh_token")?.StartsWith("refresh-") == true, "Expired TV token is renewed with the refresh token");

        sessions.Refresher = null;
        var person = new AuthenticationProperties { ExpiresUtc = clock.GetUtcNow().AddHours(8) };
        person.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "person" },
            new AuthenticationToken { Name = "expires_at", Value = clock.GetUtcNow().AddMinutes(-1).ToString("o") }]);
        var personTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "tv-user")], "test")), person, "panel");
        await sessions.StoreAsync(personTicket);
        check(!(await sessions.AuthorizationAsync(personTicket.Principal, default)).PanelAllowed, "A person session without refresh token still ends with its token");
    }
}
