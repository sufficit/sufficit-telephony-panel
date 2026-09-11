using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class BoardInventoryChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        check(new BoardConfiguration().RetainSeenResources, "Retention enabled by default including old JSON");
        var directory = Directory.CreateTempSubdirectory("panel-inventory-tests-").FullName;
        try
        {
            var path = Path.Combine(directory, "known.json");
            var store = new BoardInventoryStore(path);
            var now = DateTimeOffset.UtcNow.AddSeconds(-2);
            var row = new ObservedResource("channel-id", "eveo", "call", "PJSIP/trunk-000000ab", "Up",
                "private-caller", "private-destination", null, now, now, new Dictionary<string, string>());
            var card = new EventsPanelCardInfo { Label = "PJSIP/trunk", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/trunk"] };
            var tile = OperatorTile.Create([card], new Dictionary<EventsPanelCardInfo, List<ObservedResource>> { [card] = [row] }).Single();
            var current = await store.Observe(null, [tile], [], true);
            check(current.Single().Channels.Length == 1, "Live channel remains live");
            var json = await File.ReadAllTextAsync(path);
            check(!json.Contains("private-caller") && !json.Contains("channel-id") && !json.Contains("000000ab")
                && !json.Contains("private-destination"), "Persistence excludes channel IDs and call parties");
            store = new BoardInventoryStore(path);
            var remembered = await store.Observe(null, [], [], true);
            check(remembered.Single().Remembered && remembered.Single().Channels.Length == 0
                && remembered.Single().Status.Tone == "unknown", "Restart keeps identity, never stale activity");
            check((await store.Observe(null, [], [], false)).Length == 0, "Disabled hides remembered resources");
            check((await store.Observe(null, [], [], true)).Length == 1, "Reenabled preserves inventory");
            var trunk = new BoardRule { Label = "Tronco", Patterns = ["PJSIP/trunk"] };
            var config = new BoardConfiguration { Rules = [trunk] };
            var grouped = BoardRuleEngine.Apply(remembered, config).Single();
            check(grouped.Kind == EventsPanelCardKind.TRUNK && grouped.Remembered && grouped.Updated == now,
                "Remembered trunks still apply FOP classification and preserve last seen");
            check(BoardRuleEngine.Apply(remembered, config with { Rules = [trunk with { Kind = "hidden" }] }).Length == 0,
                "Remembered resources honor hidden rules");
            var returned = await store.Observe(null, [tile with { Rows = [row with { Kind = "peer", Peer = new() { Registration = "unregistered" } }] }], [], true);
            check(returned.Length == 1 && returned[0].Status.Tone == "offline", "New event replaces ghost without duplication");
            var tenant = Guid.CreateVersion7();
            check((await store.Observe(tenant, [], [card], true)).Length == 0, "Central history never bleeds into tenant history");
            await store.Observe(tenant, [tile], [card], true);
            check((await store.Observe(tenant, [], [card], true)).Length == 1, "Authorized company history retained");
            check((await store.Observe(tenant, [], [], true)).Length == 0, "Revoked current card hides company history");
            check((await store.Observe(Guid.CreateVersion7(), [], [card], true)).Length == 0, "Tenant histories are isolated");
            await store.Clear(null);
            check((await store.Observe(null, [], [], true)).Length == 0, "Explicit clear survives reads");
            await store.Observe(null, [tile], [], true);
            check((await store.Observe(null, [], [], true)).Length == 0, "Existing pre-clear snapshots do not repopulate history");
            var future = row with { Updated = DateTimeOffset.UtcNow.AddSeconds(2) };
            await store.Observe(null, [tile with { Rows = [future] }], [], true);
            check((await store.Observe(null, [], [], true)).Length == 1, "New post-clear event may repopulate inventory");
            check((await store.Observe(tenant, [], [card], true)).Length == 1, "Central clear leaves separate company scope unchanged");
            var queue = new EventsPanelCardInfo { Label = "Fila teste", Kind = EventsPanelCardKind.QUEUE, Channels = ["queue-id"] };
            var qtile = OperatorTile.Create([queue], new Dictionary<EventsPanelCardInfo, List<ObservedResource>>
                { [queue] = [future with { Kind = "waiting", Key = "private-waiter", Details = new Dictionary<string, string> { ["queue"] = "queue-id" } }] }).Single();
            await store.Observe(null, [qtile], [], true);
            var queueGhost = (await store.Observe(null, [], [], true)).Single(t => t.Kind == EventsPanelCardKind.QUEUE);
            check(queueGhost.Remembered && queueGhost.Rows.Length == 0, "Queue persists without retaining waiting callers");
            check(BoardRuleEngine.Apply([queueGhost], config).Single().Kind == EventsPanelCardKind.QUEUE, "SIP rules do not turn queues into peers");
            var local = tile with { Id = "local-call", Keys = ["Local/6007@context-00000001;1"] };
            await store.Observe(null, [local], [], true);
            check(!(await store.Observe(null, [], [], true)).Any(t => t.Id == "local-call"), "Ephemeral Local channels not retained as inventory");
            if (!OperatingSystem.IsWindows()) check(File.GetUnixFileMode(path) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "Inventory file private 0600");
            using var sessions = new PanelSessions(new FakeHttp(), new FakeClock());
            var access = new BoardInventoryAccess(new PanelAccess(new AnonymousState(), sessions), store);
            var denied = false;
            try { await access.Observe(null, [], [], true); } catch (UnauthorizedAccessException) { denied = true; }
            check(denied, "Anonymous cannot read inventory");
            denied = false;
            try { await access.Clear(null); } catch (UnauthorizedAccessException) { denied = true; }
            check(denied, "Anonymous cannot clear inventory");
            await File.WriteAllTextAsync(path, "broken-json");
            var corrupt = false;
            try { await new BoardInventoryStore(path).Observe(null, [tile], [], true); } catch (System.Text.Json.JsonException) { corrupt = true; }
            check(corrupt && await File.ReadAllTextAsync(path) == "broken-json", "Corrupt inventory preserved instead of silently replaced");
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class AnonymousState : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
