using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

internal static class BoardConfigurationChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        ObservedResource Row(string id, string key, string node = "eveo", string kind = "peer") =>
            new(id, node, kind, key, "unknown", "", "", null, now, now, new Dictionary<string, string>());
        OperatorTile Tile(ObservedResource row) => new(row.Id, row.Key, row.Node, EventsPanelCardKind.PEER, [row]);
        var a = Row("a", "PJSIP/wa-a"); var b = Row("b", "PJSIP/wa-b");
        var call = Row("c", "PJSIP/wa-a-000000ab", kind: "call");
        var other = Row("d", "PJSIP/wa-a", "google");
        OperatorTile[] tiles = [Tile(a), Tile(b), Tile(call), Tile(other), Tile(Row("e", "PJSIP/6007"))];
        var trunk = new BoardRule { Label = "WhatsApp", Patterns = ["PJSIP/wa-a", "PJSIP/wa-b"] };
        var config = new BoardConfiguration { Rules = [trunk] }; config.Validate();
        var result = BoardRuleEngine.Apply(tiles, config);
        check(trunk.Id.Length == 32 && trunk.Id[12] == '7', "UUIDv7 compact rule identifiers");
        check(result.Length == 3, "Aliases and active channel grouped, unrelated preserved");
        check(result.Single(t => t.Kind == EventsPanelCardKind.TRUNK && t.Node == "eveo").Rows.Length == 3, "Trunk combines only same node aliases");
        check(result.Single(t => t.Kind == EventsPanelCardKind.TRUNK && t.Node == "google").Rows.Length == 1, "Nodes remain isolated");
        check(result.Sum(t => t.Rows.Length) == tiles.Sum(t => t.Rows.Length), "Grouping does not add authorized observations");
        var hidden = new BoardRule { Label = "Ocultar", Kind = "hidden", Patterns = ["PJSIP/wa-*"] };
        check(BoardRuleEngine.Apply(tiles, config with { Rules = [hidden, trunk] }).Length == 1, "First match hides before grouping");
        check(BoardRuleEngine.Apply(tiles, config with { Rules = [trunk, hidden] }).Length == 3, "Specific rule wins when ordered first");
        check(BoardRuleEngine.Apply(tiles, config with { Rules = [hidden with { Node = "eveo" }] }).Length == 2, "Node-specific filter leaves other node intact");
        check(BoardRuleEngine.Apply(tiles, config with { ShowUnmatched = false }).Length == 2, "Unmatched resources can be hidden");
        check(BoardRuleEngine.Apply(tiles, config with { Rules = [trunk with { Enabled = false }] }).Length == 5, "Disabled rules do not match");
        check(BoardRuleEngine.EndpointKey("SIP/in-dtr-67", true) == "SIP/in-dtr-67", "Numbered endpoint is not stripped");
        check(!BoardRuleEngine.Matches(trunk, "SIP/wa-a", "eveo"), "SIP and PJSIP are not conflated");
        check(BoardRuleEngine.Apply([], config).Length == 0, "Configured identifiers do not invent unauthorized tiles");
        var mixed = new OperatorTile("mixed", "Portal aliases", "eveo", EventsPanelCardKind.PEER, [a, b]);
        var split = BoardRuleEngine.Apply([mixed], config with { Rules = [trunk with { Patterns = ["PJSIP/wa-a"] }] });
        check(split.Length == 2 && split.All(t => t.Rows.Length == 1), "Partial match splits original multi-alias card safely");
        check(BoardRuleEngine.Apply([mixed, mixed], config).Single().Rows.Length == 2, "Duplicate row IDs deduplicated in a group");
        foreach (var invalid in new[] { "*", "PJSIP/*", "PJSIP/a*b", "PJSIP/[ab]", "PJSIP/a b", "PJSIP/a?" })
        {
            var rejected = false;
            try { (config with { Rules = [trunk with { Patterns = [invalid] }] }).Validate(); }
            catch (InvalidOperationException) { rejected = true; }
            check(rejected, "Unsafe/ambiguous pattern rejected: " + invalid);
        }
        var directory = Directory.CreateTempSubdirectory("panel-rule-tests-").FullName;
        try
        {
            var path = Path.Combine(directory, "rules.json");
            var store = new BoardRuleStore(path);
            using var sessions = new PanelSessions(new FakeHttp(), new FakeClock());
            var access = new BoardConfigurationAccess(new PanelAccess(new AnonymousState(), sessions), store, NullLogger<BoardConfigurationAccess>.Instance);
            var denied = false;
            try { await access.Save(config); } catch (UnauthorizedAccessException) { denied = true; }
            check(denied && !File.Exists(path), "Anonymous settings write denied before touching storage");
            denied = false;
            try { await access.Read(); } catch (UnauthorizedAccessException) { denied = true; }
            check(denied, "Anonymous settings read denied");
            check((await store.Read()).ShowUnmatched && !File.Exists(path), "Empty installation preserves display without automatic writes");
            var saved = await store.Save(config);
            check(saved.Revision.Length == 32 && saved.Revision[12] == '7', "Save gets fresh UUIDv7 revision");
            var read = await new BoardRuleStore(path).Read();
            check(read.Rules.Single().Label == "WhatsApp" && read.Revision == saved.Revision, "Persistence survives new store instance");
            if (!OperatingSystem.IsWindows()) check(File.GetUnixFileMode(path) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "Settings file private 0600");
            var conflict = false;
            try { await store.Save(config); } catch (InvalidOperationException) { conflict = true; }
            check(conflict && (await store.Read()).Revision == saved.Revision, "Stale save rejected without overwriting newer rules");
            saved.Rules[0].Patterns[0] = "PJSIP/mutated";
            check((await store.Read()).Rules[0].Patterns[0] == "PJSIP/wa-a", "Read result cannot mutate store state");
            var latest = await store.Read();
            async Task<bool> TrySave() { try { await store.Save(latest); return true; } catch (InvalidOperationException) { return false; } }
            var concurrent = await Task.WhenAll(TrySave(), TrySave());
            check(concurrent.Count(x => x) == 1, "Only one concurrent writer with same revision succeeds");
            await File.WriteAllTextAsync(path, "broken-json");
            var corrupt = false;
            try { await new BoardRuleStore(path).Save(config); } catch (System.Text.Json.JsonException) { corrupt = true; }
            check(corrupt && await File.ReadAllTextAsync(path) == "broken-json", "Corrupt configuration is not overwritten silently");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
    private sealed class AnonymousState : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
