using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class AcdBoardChecks
{
    private sealed class Anonymous : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
    public static async Task Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.NewGuid(); var other = Guid.NewGuid(); var node = Guid.NewGuid();
        var queue = new AcdBoardQueue(Guid.NewGuid().ToString("N"), "Homologação Sufficit — 6599", 1, 2, now.AddSeconds(-5));
        var visit = new AcdBoardVisit(Guid.NewGuid().ToString("N"), queue.QueueId, "waiting", now.AddSeconds(-5), "1789.1", "SIP/0000006007-000000ab", "6007");
        var snapshot = new AcdBoardSnapshot(context.ToString("N"), node.ToString("N"), now, true, false, [queue], [visit]);
        var channel = new ObservedResource("ami-call", "google-voip", "call", visit.ChannelName!, "Up", "6007", "6599", context.ToString("N"), now.AddSeconds(-7), now,
            new Dictionary<string, string> { ["uniqueid"] = "1789.1", ["linkedid"] = "1789.1" });
        var sample = new AcdBoardSample("google-voip", context, snapshot, true);
        AcdBoardProjection Project(AcdBoardSample? data = null, params ObservedResource[] channels) =>
            AcdBoardProjection.Create([data ?? sample], channels, context, now);
        check(AcdBoardReader.ValidSnapshot(snapshot, context, node), "ACD snapshot accepts correct compact IDs");
        check(!AcdBoardReader.ValidSnapshot(snapshot, other, node) && !AcdBoardReader.ValidSnapshot(snapshot, context, other), "ACD response context/node mismatch rejected");
        check(!AcdBoardReader.ValidSnapshot(snapshot with { Queues = [queue, queue] }, context, node), "Duplicate ACD queue identities rejected");
        check(!AcdBoardReader.ValidSnapshot(snapshot with { Queues = [queue with { Waiting = -1 }] }, context, node), "Negative ACD counts rejected");
        check(!AcdBoardReader.ValidSnapshot(snapshot with { Visits = [visit with { QueueId = other.ToString("N") }] }, context, node), "Visit cannot reference another queue");
        check(!AcdBoardReader.ValidSnapshot(snapshot with { Visits = [visit with { ChannelId = "bad\nchannel" }] }, context, node), "Control characters rejected");
        check(AcdBoardProjection.Create([sample], [channel], other, now).Rows.Length == 0, "Context filter never exposes another ACD source");
        var projected = Project(null, channel);
        check(projected.Rows.Single(r => r.Kind == "waiting").Details["linkedid"] == "1789.1", "Exact node/context/uniqueid links ARI visit to AMI call");
        foreach (var wrong in new[] { channel with { Node = "eveo-voip" }, channel with { Account = other.ToString("N") }, channel with { Account = null },
            channel with { Details = new Dictionary<string, string> { ["uniqueid"] = "another", ["linkedid"] = "1789.1" } }, channel with { Key = "SIP/another-000000ab" } })
            check(Project(null, wrong).Rows.Single(r => r.Kind == "waiting").Details["correlation"] == "unmatched", "No node/context/caller/linkedid guessing for ACD");
        check(Project(null, channel, channel with { Id = "duplicate" }).Rows.Single(r => r.Kind == "waiting").Details["correlation"] == "unmatched", "Ambiguous exact channel rejected");
        var nativeCard = new EventsPanelCardInfo { Label = "Hugo", Kind = EventsPanelCardKind.PEER, Channels = ["SIP/0000006007"] };
        var observed = AuthorizedObservation.Create([nativeCard], [channel], context);
        observed.AddAcd(projected);
        var tiles = BoardCallFlow.Attach(OperatorTile.Create(observed.Cards, observed.ByCard), observed.Rows);
        check(tiles.Single(t => t.Title == "Hugo").Flows.Single().Destination == queue.Title, "Extension destination resolves to ACD queue title");
        var tile = tiles.Single(t => t.Acd is not null);
        check(tile.QueueCalls.Length == 1 && tile.DetailChannels.Single().Id == channel.Id, "Queue flow includes exact live channel once");
        check(tile.Keys.Single().StartsWith("acd:" + context.ToString("N")) && tile.Node == "google-voip", "ACD identity retains context and observed node");
        foreach (var phase in new[] { "greeting", "waiting", "announcing", "offering", "connected" })
        {
            var changed = Project(sample with { Snapshot = snapshot with { Visits = [visit with { Phase = phase }] } }, channel);
            check(changed.Rows.Single(r => r.Kind == "waiting").State == (phase == "connected" ? "answered" : phase), "ACD phase preserved: " + phase);
        }
        foreach (var stale in new[] { sample with { Available = false }, sample with { Snapshot = snapshot with { ObservedAt = now.AddSeconds(-21) } },
            sample with { Snapshot = snapshot with { ObservedAt = now.AddSeconds(6) } }, sample with { Snapshot = snapshot with { ControllerConnected = false } },
            sample with { Snapshot = snapshot with { ObserverOnly = true } } })
        {
            var result = Project(stale, channel);
            check(result.Rows.Length == 1 && result.Rows[0].State == "stale" && !result.Rows[0].Details.ContainsKey("pending"), "Stale/unavailable ACD retains catalog without live counts or callers");
        }
        var legacy = Project(sample with { Snapshot = snapshot with { Visits = null } });
        check(legacy.Rows.Single().Details["pending"] == "1" && legacy.Rows.Single().Details["correlation"] == "unavailable", "Legacy controller supplies counts without fabricated channel");
        check(OperatorTile.Create(legacy.ByCard.Keys, legacy.ByCard).Single().HasObservedActivity, "Observed-activity filter includes legacy authoritative queue count");
        var empty = Project(sample with { Snapshot = snapshot with { Queues = [queue with { Waiting = 0 }], Visits = [] } });
        check(empty.Rows.Length == 1 && empty.Rows[0].Details["pending"] == "0", "Terminal snapshot removes visit and preserves empty queue");
        var clock = new FakeClock { Now = now }; var http = new FakeHttp { Body = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
        var file = Path.Combine(Path.GetTempPath(), "acd-board-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            await File.WriteAllTextAsync(file, new string('x', 48));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var source = new AcdBoardSource("google-voip", node, [context], "http://172.19.254.250:18189/", file);
            check(AcdBoardReader.ValidSource(source), "Private VPN source accepted");
            foreach (var url in new[] { "http://0.0.0.0:18189", "http://8.8.8.8", "https://example.com", "http://user:pass@127.0.0.1", "http://127.0.0.1/?token=x", "file:///tmp/acd" })
                check(!AcdBoardReader.ValidSource(source with { BaseUrl = url }), "Unsafe ACD source rejected");
            using var reader = new AcdBoardReader([source], http, clock);
            var all = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => reader.Read(context, default)));
            check(http.Requests == 1 && all.All(r => r.Single().Available), "Concurrent panels share one ACD snapshot fetch");
            check((await reader.Read(other, default)).Length == 0 && http.Requests == 1, "Unconfigured context causes no private request");
            clock.Now = now.AddSeconds(3); http.Status = HttpStatusCode.ServiceUnavailable;
            var failed = (await reader.Read(context, default)).Single();
            check(!failed.Available && failed.Snapshot is not null, "Failed refresh retains catalog but marks activity unavailable");
            clock.Now = now.AddSeconds(6); http.Status = HttpStatusCode.Forbidden;
            check((await reader.Read(context, default)).Single().Snapshot is null, "Upstream authorization failure drops cached snapshot");
            clock.Now = now.AddSeconds(9); http.Status = HttpStatusCode.OK;
            http.Body = JsonSerializer.Serialize(snapshot with { ContextId = other.ToString("N") }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            check(!(await reader.Read(context, default)).Single().Available, "Wrong-context private response never becomes live");
            clock.Now = now.AddSeconds(12); http.Body = "not-json";
            check(!(await reader.Read(context, default)).Single().Available, "Malformed private response degrades independently");
            clock.Now = now.AddSeconds(15); http.Body = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            check((await reader.Read(context, default)).Single().Available, "Private snapshot recovers on subsequent refresh");
            using (var partial = new AcdBoardReader([source, source with { Node = "eveo-voip", NodeId = other }], http, clock))
            {
                var mixed = await partial.Read(context, default);
                check(mixed.Single(s => s.Node == "google-voip").Available && !mixed.Single(s => s.Node == "eveo-voip").Available,
                    "One invalid node snapshot does not suppress another node");
            }
            using var sessions = new PanelSessions(http, clock);
            var access = new AcdBoardAccess(new PanelAccess(new Anonymous(), sessions), reader);
            var requests = http.Requests;
            try { await access.Read(null, default); throw new Exception("Anonymous ACD access granted"); }
            catch (UnauthorizedAccessException) { check(http.Requests == requests, "Anonymous viewer denied before cached/private ACD data"); }
        }
        finally { File.Delete(file); }
    }
}
