using System.Text.Json;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Tools;

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
if (args is ["extract", var source])
{
    Console.WriteLine(JsonSerializer.Serialize(FopRuleCatalog.Extract(await File.ReadAllTextAsync(source)), json));
}
else if (args is ["verify", var file])
{
    var value = await new BoardRuleStore(Path.GetFullPath(file)).Read();
    value.Validate();
    Console.WriteLine($"revision={value.Revision}; rules={value.Rules.Length}; patterns={value.Rules.Sum(r => r.Patterns.Length)}");
    foreach (var rule in value.Rules) Console.WriteLine($"{rule.Label}: {rule.Patterns.Length}");
}
else if (args is ["apply", var catalogPath, var target, var expected])
{
    // Administrative offline tool: the caller must stop the single panel writer.
    // No live API credential or browser authorization is bypassed by an endpoint.
    if (!Path.IsPathFullyQualified(target)) throw new InvalidOperationException("Absolute target path required.");
    if (expected == "absent" && File.Exists(target)) throw new InvalidOperationException("Preflight expected no file; refusing concurrent change.");
    var store = new BoardRuleStore(target);
    var current = await store.Read();
    if (current.Revision != (expected == "absent" ? "" : expected)) throw new InvalidOperationException("Revision changed since preflight.");
    var catalog = JsonSerializer.Deserialize<BoardConfiguration>(await File.ReadAllTextAsync(catalogPath), json)
        ?? throw new InvalidOperationException("Invalid catalog.");
    var merged = FopRuleCatalog.Merge(current, catalog);
    if (merged.Rules.Length == current.Rules.Length) Console.WriteLine("No changes; catalog already included.");
    else
    {
        var saved = await store.Save(merged);
        Console.WriteLine($"Saved revision={saved.Revision}; rules={saved.Rules.Length}; patterns={saved.Rules.Sum(r => r.Patterns.Length)}");
    }
}
else throw new ArgumentException("Usage: extract <controller> | verify <file> | apply <catalog> <absolute-target> <expected-revision|absent>");
