using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Sufficit.EndPoints.Security;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class RepresentativePortfolioChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var own = Guid.NewGuid(); var child = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var direct = new CurrentAuthorization(false, [own], [own]);
        var queried = new List<Guid>();
        Task<IEnumerable<Guid>> Read(Guid id, CancellationToken ct)
        { queried.Add(id); return Task.FromResult<IEnumerable<Guid>>(id == own ? [own, child, child, Guid.Empty] : [foreign]); }
        var expanded = await TelephonyPortfolio.Expand(direct, Read, default);
        check(expanded.PanelContexts.SetEquals([own, child]), "Representative keeps self and distinct direct customers");
        check(queried.SequenceEqual([own]) && !expanded.CanRead(foreign), "Portfolio expansion never traverses represented customers");
        check(expanded.CanMonitor(own) && !expanded.CanMonitor(child) && !expanded.Manager, "Representation never grants monitor or manager");
        queried.Clear();
        await TelephonyPortfolio.Expand(CurrentAuthorization.None, Read, default);
        await TelephonyPortfolio.Expand(new(true, [], []), Read, default);
        check(queried.Count == 0, "Denied and global permissions do not query portfolios");
        var revoked = await TelephonyPortfolio.Expand(direct, (_, _) => Task.FromResult<IEnumerable<Guid>>([]), default);
        check(!revoked.CanRead(child) && revoked.CanRead(own), "Ended relationship disappears without losing own access");
        try { await TelephonyPortfolio.Expand(direct, (_, _) => throw new InvalidOperationException("unavailable"), default); check(false, "Portfolio failure must propagate"); }
        catch (InvalidOperationException) { check(true, "Unavailable authority never produces expanded grants"); }

        var relationClock = new FakeClock();
        using var relations = new TelephonyPortfolioCache(relationClock);
        queried.Clear();
        await relations.Read(own, Read, default);
        await relations.Read(own, Read, default);
        check(queried.Count == 1, "Repeated API card reads share the short portfolio cache");
        relationClock.Now += TimeSpan.FromSeconds(11);
        try { await relations.Read(own, (_, _) => throw new InvalidOperationException("unavailable"), default); check(false, "Stale relation survived"); }
        catch (InvalidOperationException) { check(true, "Expired API relationships fail closed"); }
        check(!(await relations.Read(own, (_, _) => Task.FromResult<IEnumerable<Guid>>([]), default)).Any(), "API cache accepts relationship removal");

        var http = new FakeHttp { Body = JsonSerializer.Serialize(new[] { own, child }) };
        var clock = new FakeClock();
        using var client = new PanelPortfolioClient(http, clock);
        check((await client.Expand(direct, "token", default)).CanRead(child), "Panel reads expanded portfolio from API");
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.Expand(direct, "token", default)));
        check(http.Requests == 1, "Concurrent portfolio reads share bounded cache");
        clock.Now += TimeSpan.FromSeconds(61); http.Status = HttpStatusCode.ServiceUnavailable;
        try { await client.Expand(direct, "token", default); check(false, "Expired portfolio must not survive failure"); }
        catch (HttpRequestException) { check(true, "No stale portfolio on failed refresh"); }
        http.Status = HttpStatusCode.OK; http.Body = JsonSerializer.Serialize(new[] { own });
        check(!(await client.Expand(direct, "token", default)).CanRead(child), "Portfolio revocation applies to same token");
        http.Body = JsonSerializer.Serialize(new[] { own, Guid.Empty });
        try { await client.Expand(direct, "another-token", default); check(false, "Global escalation must fail"); }
        catch (InvalidOperationException) { check(true, "Portfolio cannot turn a contextual grant global"); }
        check(!(await client.Expand(CurrentAuthorization.None, "token", default)).PanelAllowed, "Cached portfolio cannot revive revoked direct grant");

        var sessionHttp = new SessionHttp(own, child);
        using var sessions = new PanelSessions(sessionHttp, clock);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new("sub", "representative")], "panel"));
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
        properties.StoreTokens([new() { Name = "access_token", Value = "session-token" },
            new() { Name = "expires_at", Value = clock.Now.AddHours(1).ToString("O") }]);
        await sessions.StoreAsync(new AuthenticationTicket(principal, properties, "panel"));
        check((await sessions.AuthorizationAsync(principal, default)).CanRead(child), "Actual panel session sees represented context");
        check(await sessions.InitialContextAsync(principal, own, default) is null, "Representative landing opens aggregate instead of own-only context");
        check(await sessions.InitialContextAsync(principal, child, default) == child, "Explicit represented company remains selectable");

        EventsPanelCardInfo Card(string key) => new() { Label = "Same name", Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/" + key] };
        var ownCard = Card("own"); var childCard = Card("child");
        var catalogues = new Dictionary<Guid, EventsPanelCardInfo[]> { [own] = [ownCard, Card("idle")], [child] = [childCard, Card("idle")] };
        ObservedResource Peer(string id, string key, string account = "") => new(id, "pbx", "peer", "SIP/" + key, "available", "", "", account, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<string, string>());
        var rows = new[] { Peer("own", "own"), Peer("child", "child"), Peer("ambiguous", "idle"), Peer("foreign", "child", foreign.ToString()), Peer("unknown", "foreign") };
        var view = AuthorizedObservation.ForPortfolio(catalogues, rows, expanded);
        check(view.Cards.Length == 4 && view.Rows.Length == 2, "Aggregate includes idle catalogues, excludes foreign and ambiguous observations");
        check(view.Rows.All(r => Guid.TryParse(r.Account, out var id) && expanded.CanRead(id)), "Attributed status carries exact catalogue context");
        var tiles = OperatorTile.Create(view.Cards, view.ByCard, true, view.CardContexts);
        check(tiles.Length == 4 && tiles.Select(t => t.Id).Distinct().Count() == 4, "Idle same-name endpoints retain separate company identities");
        view = AuthorizedObservation.ForPortfolio(catalogues, rows, direct);
        check(view.Cards.Length == 2 && view.Rows.All(r => r.Account == own.ToString("N")), "Revoked catalogue is excluded even when still cached");
    }
    private sealed class SessionHttp(Guid own, Guid child) : HttpMessageHandler, IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            object body = request.RequestUri!.AbsolutePath == "/telephony/panel/contexts" ? new[] { own, child }
                : new { sub = "representative", authorization = new { schemaVersion = 1, grants = new[] { "telephony.panel:" + own } } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body)) });
        }
    }
}
