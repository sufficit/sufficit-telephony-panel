using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sufficit.Telephony.Panel.Security;

static class ImpersonationChecks
{
    private sealed class Identity : HttpMessageHandler, IHttpClientFactory
    {
        public bool Refuse;
        public string? LastForm;
        public string[] Grants = [];
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/connect/token"))
            {
                LastForm = await request.Content!.ReadAsStringAsync(ct);
                return Refuse
                    ? Json(new { error = "access_denied", error_description = "Administrators, managers and protected accounts cannot be impersonated." }, HttpStatusCode.BadRequest)
                    : Json(new { access_token = "person-token", token_type = "Bearer", expires_in = 1800 });
            }
            if (path.StartsWith("/connect/userinfo?entitlements=current"))
                return Json(new { sub = "person", authorization = new { schemaVersion = 1, grants = Grants } });
            if (path.StartsWith("/connect/userinfo")) return Json(new { sub = "person", name = "Ana Cliente" });
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(JsonSerializer.Serialize(body)) };
    }

    public static async Task Run(Action<bool, string> check)
    {
        var context = Guid.NewGuid();
        var identity = new Identity { Grants = ["telephony.panel:" + context.ToString("N"), "telephony.monitor:" + context.ToString("N")] };
        var clock = new FakeClock();
        var service = new PanelImpersonation(identity, new ConfigurationBuilder().Build(), clock, NullLogger<PanelImpersonation>.Instance);

        var (issued, refusal) = await service.ExchangeAsync("manager-token", "1d4c0b8e-0000-4000-8000-000000000001", default);
        check(issued is { AccessToken: "person-token", Name: "Ana Cliente" } && refusal is null, "Accepted exchange returns the person's token and name");
        check(identity.LastForm!.Contains("impersonated_subject=1d4c0b8e-0000-4000-8000-000000000001") && identity.LastForm.Contains("subject_token=manager-token")
            && identity.LastForm.Contains("token-exchange"), "The manager's own token is exchanged for the named person");
        identity.Refuse = true;
        (issued, refusal) = await service.ExchangeAsync("manager-token", "1d4c0b8e-0000-4000-8000-000000000001", default);
        check(issued is null && refusal!.StartsWith("access_denied"), "Identity refusal reason reaches the manager");
        check(ImpersonationPage.Refused(refusal!, "").Contains("gestores"), "Refusal page explains privileged targets");
        check(ImpersonationPage.Refused("invalid_grant: Impersonation requires a sign-in with a second factor.", "").Contains("segundo fator"), "Refusal page explains the second factor");

        // The person's session: real panel grants, never monitor or manager.
        using var sessions = new PanelSessions(identity, clock);
        var properties = new AuthenticationProperties { ExpiresUtc = clock.GetUtcNow().AddMinutes(30) };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "person-token" },
            new AuthenticationToken { Name = "expires_at", Value = clock.GetUtcNow().AddMinutes(30).ToString("o") }]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "person"), new Claim(PanelImpersonation.ActorClaim, "manager")], "impersonation"));
        await sessions.StoreAsync(new AuthenticationTicket(principal, properties, "panel"));
        var permissions = await sessions.AuthorizationAsync(principal, default);
        check(permissions.CanRead(context) && !permissions.CanMonitor(context) && !permissions.Manager, "Impersonated session reads the person's context but never listens or whispers");
        check(PanelImpersonation.IsImpersonating(principal), "Impersonated principal is recognized");
    }
}
