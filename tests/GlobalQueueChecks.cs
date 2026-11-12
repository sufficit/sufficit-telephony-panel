using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class GlobalQueueChecks
{
    private sealed class PrincipalProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
    }
    private sealed class Http(Guid node, Guid first, Guid second, FakeClock clock) : HttpMessageHandler, IHttpClientFactory
    {
        public int Aggregates;
        public int Snapshots;
        public string? AggregateBody;
        public HttpStatusCode AggregateStatus = HttpStatusCode.OK;
        public bool Global = true;
        public int Waiting = 3;
        public List<Guid> Requested { get; } = [];
        public QueueBoardSnapshot Sample(Guid context) => new(context.ToString("N"), node.ToString("N"), clock.Now, true, false,
            [new QueueBoardQueue(context.ToString("N"), "Principal", Waiting, 0, clock.Now)], []);
        public HttpClient CreateClient(string name) => new(this, false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            object body;
            if (path == "/connect/userinfo") body = new { sub = "global-queues", role = Global ? "manager" : "customer",
                authorization = new { schemaVersion = 1, grants = Global ? Array.Empty<string>() : new[] { "telephony.panel:" + first, "telephony.panel:" + second } } };
            else if (path == "/telephony/panel/contexts") body = new[] { first, second };
            else if (path == $"/api/queue/nodes/{node:N}/snapshot")
            {
                Aggregates++;
                body = new { nodeId = node.ToString("N"), snapshots = new[] { Sample(first), Sample(second) } };
                return Task.FromResult(new HttpResponseMessage(AggregateStatus) { Content = new StringContent(AggregateBody ??
                    JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))) });
            }
            else
            {
                var context = Guid.Parse(path.Split('/')[4]);
                Snapshots++; Requested.Add(context);
                body = Sample(context);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))) });
        }
    }
    public static async Task Run(Action<bool, string> check)
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid(); var node = Guid.NewGuid();
        var clock = new FakeClock(); var http = new Http(node, first, second, clock);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new("sub", "global-queues")], "panel"));
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
        properties.StoreTokens([new() { Name = "access_token", Value = "global-queue-test" },
            new() { Name = "expires_at", Value = clock.Now.AddHours(1).ToString("O") }]);
        using var sessions = new PanelSessions(http, clock);
        await sessions.StoreAsync(new AuthenticationTicket(principal, properties, "panel"));
        var key = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(key, new string('x', 40));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(key, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var source = new QueueBoardSource("test-node", node, [first], "http://127.0.0.1/", key, true);
            using var reader = new QueueBoardReader([source], http, clock);
            foreach (var status in new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden })
            {
            http.AggregateStatus = status;
            using (var legacyReader = new QueueBoardReader([source], http, clock))
            {
                var legacyAccess = new QueueBoardAccess(new PanelAccess(new PrincipalProvider(principal), sessions), legacyReader);
                var legacy = await legacyAccess.Read(null, default);
                check(legacy.Any(s => s.ContextId == first && s.Available) && legacy.Any(s => s.ContextId == Guid.Empty && s.HttpStatus == (int)status && !s.Available),
                    "Old controller preserves configured queue snapshots with explicit incomplete-global-coverage warning");
                check(legacy.All(s => s.ContextId != second), "Legacy compatibility does not invent unconfigured company coverage");
            }
            }
            http.AggregateStatus = HttpStatusCode.OK; http.Aggregates = http.Snapshots = 0; http.Requested.Clear();
            var access = new QueueBoardAccess(new PanelAccess(new PrincipalProvider(principal), sessions), reader);
            var samples = await access.Read(null, default);
            check(samples.Length == 2 && samples.All(s => s.Available) && samples.Select(s => s.ContextId).ToHashSet().SetEquals([first, second]),
                "Global overview discovers active companies beyond fixed labs without AMI queue events");
            check(http.Snapshots == 0, "Global read uses one aggregate request without per-company fan-out");
            var projection = QueueBoardProjection.Create(samples, [], null, clock.Now);
            check(BoardTotals.Waiting(OperatorTile.Create(projection.ByCard.Keys, projection.ByCard)) == 6, "Global board sums current waiting counts from all discovered companies");
            await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => access.Read(null, default)));
            check(http.Aggregates == 1 && http.Snapshots == 0, "Concurrent global viewers share cached aggregate and snapshots");
            clock.Now += TimeSpan.FromSeconds(3); http.Waiting = 1;
            samples = await access.Read(null, default);
            projection = QueueBoardProjection.Create(samples, [], null, clock.Now);
            check(BoardTotals.Waiting(OperatorTile.Create(projection.ByCard.Keys, projection.ByCard)) == 2 && http.Aggregates == 2, "Queue totals change on the next aggregate refresh");
            await access.Read(second, default);
            check(http.Aggregates == 2, "Selected company never performs global discovery");
            foreach (var body in new[] {
                JsonSerializer.Serialize(new { nodeId = Guid.NewGuid().ToString("N"), snapshots = new[] { http.Sample(first) } }),
                JsonSerializer.Serialize(new { nodeId = node.ToString("N"), snapshots = new[] { http.Sample(Guid.Empty) } }),
                JsonSerializer.Serialize(new { nodeId = node.ToString("N"), snapshots = new[] { http.Sample(first), http.Sample(first) } }),
                JsonSerializer.Serialize(new { nodeId = node.ToString("N"), snapshots = Enumerable.Repeat(http.Sample(first), 1001).ToArray() }),
                JsonSerializer.Serialize(new { nodeId = node.ToString("N"), snapshots = new[] { http.Sample(first) with { NodeId = Guid.NewGuid().ToString("N") } } }),
                "not-json" })
            {
                clock.Now += TimeSpan.FromSeconds(3); http.AggregateBody = body;
                samples = await access.Read(null, default);
                check(samples.Length > 0 && samples.All(s => !s.Available) && QueueBoardProjection.Create(samples, [], null, clock.Now).Rows.All(r => r.State != "current"),
                    "Invalid aggregate is visible and cannot retain activity as current");
            }
            clock.Now += TimeSpan.FromSeconds(3); http.AggregateBody = null; http.AggregateStatus = HttpStatusCode.NotFound;
            samples = await access.Read(null, default);
            check(samples.All(s => !s.Available && s.HttpStatus == 404), "Legacy controller missing aggregate produces explicit source failure");
            clock.Now += TimeSpan.FromSeconds(3); http.AggregateStatus = HttpStatusCode.Forbidden;
            samples = await access.Read(null, default);
            check(samples.Any(s => s.ContextId == Guid.Empty && s.Snapshot is null && !s.Available) && samples.All(s => s.ContextId != second), "Upstream aggregate denial drops cached global company data; only separately authorized fixed snapshots can return");
            clock.Now += TimeSpan.FromSeconds(3); http.AggregateStatus = HttpStatusCode.OK;
            check((await access.Read(null, default)).Length == 2, "Global aggregate recovers without restarting the panel");
            clock.Now += TimeSpan.FromSeconds(61); http.Global = false;
            var discoveries = http.Aggregates;
            samples = await access.Read(null, default);
            check(samples.All(s => s.ContextId != Guid.Empty) && http.Aggregates == discoveries, "Restricted portfolio never invokes global aggregate after role revocation");
        }
        finally { File.Delete(key); }
    }
}
