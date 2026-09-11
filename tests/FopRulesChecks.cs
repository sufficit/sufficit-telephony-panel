using System.Text.Json;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Tools;

internal static class FopRulesChecks
{
    public static void Run(Action<bool, string> check)
    {
        var catalog = JsonSerializer.Deserialize<BoardConfiguration>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fop-board-rules.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        catalog.Validate();
        check(catalog.Rules.Length == 14 && catalog.Rules.Sum(r => r.Patterns.Length) == 130, "Complete FOP trunk catalog retained");
        check(catalog.Rules.Single(r => r.Label == "INBOUND").Patterns.Length == 45, "Inbound is not truncated at old 32 limit");
        check(catalog.Rules.All(r => r.Kind == "trunk" && r.Node == "" && r.Enabled), "All source trunk groups enabled on every node");
        string? Match(string key) => catalog.Rules.FirstOrDefault(r => BoardRuleEngine.Matches(r, key, "google-voip"))?.Label;
        check(Match("SIP/in-dtr-67") == "IN-DATORA", "Specific incoming group wins over fallback");
        check(Match("SIP/in-new-provider") == "INBOUND", "Inbound stem made explicit");
        check(Match("SIP/out-new-provider") == "OUTBOUND", "Outbound stem made explicit");
        check(Match("PJSIP/gateway-whatsapp-quepasa-inbound") == "WHATSAPP", "WhatsApp PJSIP retained");
        check(Match("IAX2/dundi-internal") == "GATEWAYS", "IAX2 gateway retained");
        check(Match("PJSIP/in-dtr-67") is null, "No invented protocol conversion");
        check(Match("SIP/mixed-marvin-upbytes") == "INBOUND", "Source-order overlap documented as first match");
        var merged = FopRuleCatalog.Merge(new(), catalog);
        check(FopRuleCatalog.Merge(merged, catalog).Rules.Length == 14, "Re-import is idempotent");
        var userRule = new BoardRule { Label = "Internal hide", Kind = "hidden", Patterns = ["Local/internal-*"] };
        var existing = new BoardConfiguration { Rules = [userRule], ShowUnmatched = false };
        var preserved = FopRuleCatalog.Merge(existing, catalog);
        check(preserved.Rules[0] == userRule && !preserved.ShowUnmatched, "User rules and unmatched preference preserved");
        var conflict = false;
        try { FopRuleCatalog.Merge(new BoardConfiguration { Rules = [catalog.Rules[0] with { Patterns = ["SIP/custom"] }] }, catalog); }
        catch (InvalidOperationException) { conflict = true; }
        check(conflict, "Conflicting existing rule is not replaced");
        var many = new BoardConfiguration { Rules = [userRule with { Patterns = Enumerable.Range(0, 64).Select(i => "SIP/test-" + i).ToArray() }] };
        many.Validate(); check(true, "64 patterns supported");
        var excessive = false;
        try { (many with { Rules = [userRule with { Patterns = Enumerable.Range(0, 65).Select(i => "SIP/test-" + i).ToArray() }] }).Validate(); }
        catch (InvalidOperationException) { excessive = true; }
        check(excessive, "65 patterns rejected");
        var extracted = FopRuleCatalog.Extract("writer.WriteLine(\"user=private-value\"); private void writeTrunks(StreamWriter writer) { writer.WriteLine($\"[GATEWAYS]\"); writer.WriteLine($\"type=trunk\"); writer.WriteLine($\"label=GATEWAYS\"); writer.WriteLine($\"channel=IAX2/dundi-internal\"); writer.WriteLine($\"context=default\"); }");
        check(extracted.Rules.Single().Patterns.SequenceEqual(new[] { "IAX2/dundi-internal" }), "Extractor excludes users and dialplan context");
    }
}
