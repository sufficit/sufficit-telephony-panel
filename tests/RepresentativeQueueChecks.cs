using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class RepresentativeQueueChecks
{
    private sealed class PrincipalProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
    }
    private sealed class Http(Guid own, Guid child, Guid node, FakeClock clock) : HttpMessageHandler, IHttpClientFactory
    {
        public bool Revoked;
        public List<Guid> Requested { get; } = [];
        public HttpClient CreateClient(string name) => new(this, false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            object body;
            if (path == "/connect/userinfo") body = new { sub = "representative", authorization = new { schemaVersion = 1, grants = new[] { "telephony.panel:" + own } } };
            else if (path == "/telephony/panel/contexts") body = Revoked ? new[] { own } : new[] { own, child };
            else
            {
                var context = Guid.Parse(path.Split('/')[4]);
                Requested.Add(context);
                body = new QueueBoardSnapshot(context.ToString("N"), node.ToString("N"), clock.Now, true, false,
                    [new QueueBoardQueue(context.ToString("N"), "Principal", 0, 0, clock.Now)], []);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))) });
        }
    }
    public static async Task Run(Action<bool, string> check)
    {
        var own = Guid.NewGuid(); var child = Guid.NewGuid(); var foreign = Guid.NewGuid(); var node = Guid.NewGuid();
        var clock = new FakeClock();
        var http = new Http(own, child, node, clock);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new("sub", "representative")], "panel"));
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
        properties.StoreTokens([new() { Name = "access_token", Value = "representative-queue-test" },
            new() { Name = "expires_at", Value = clock.Now.AddHours(1).ToString("O") }]);
        using var sessions = new PanelSessions(http, clock);
        await sessions.StoreAsync(new AuthenticationTicket(principal, properties, "panel"));
        var key = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(key, new string('x', 40));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(key, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            using var reader = new QueueBoardReader([new("test-node", node, [foreign], "http://127.0.0.1/", key, true)], http, clock);
            var access = new QueueBoardAccess(new PanelAccess(new PrincipalProvider(principal), sessions), reader);
            var samples = await access.Read(null, default);
            check(samples.Select(s => s.ContextId).ToHashSet().SetEquals([own, child]) && samples.All(s => s.Available),
                "Representative overview loads own and represented on-demand queues");
            check(http.Requested.ToHashSet().SetEquals([own, child]), "Aggregate never queries a configured foreign company");
            var projection = QueueBoardProjection.Create(samples, [], null, clock.Now);
            var tiles = OperatorTile.Create(projection.ByCard.Keys, projection.ByCard, true);
            check(tiles.Length == 2 && tiles.Select(t => t.Id).Distinct().Count() == 2, "Same-named portfolio queues remain distinct visible tiles");
            check((await access.Read(child, default)).Single().ContextId == child, "Individual company queue selection still works");
            var requests = http.Requested.Count;
            try { await access.Read(foreign, default); check(false, "Foreign queue access must fail"); }
            catch (UnauthorizedAccessException) { check(http.Requested.Count == requests, "Foreign selection fails before private reads"); }
            clock.Now = clock.Now.AddSeconds(61); http.Revoked = true; http.Requested.Clear();
            samples = await access.Read(null, default);
            check(samples.Length == 1 && samples[0].ContextId == own && !http.Requested.Contains(child), "Removed representative relationship is no longer queried or rendered");
        }
        finally { File.Delete(key); }
    }
}
