using System.Text.RegularExpressions;
using Sufficit.Telephony.Panel.Operations;

namespace Sufficit.Telephony.Panel.Tools;

public static class FopRuleCatalog
{
    public static BoardConfiguration Extract(string source)
    {
        var start = source.IndexOf("private void writeTrunks(StreamWriter writer)", StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Expected writeTrunks method was not found.");
        // Only literal trunk declarations are extracted. Never parse GetConfig/users.
        var body = source[start..];
        var rules = new List<BoardRule>();
        string? label = null;
        var patterns = new List<string>();
        void Finish()
        {
            if (label is null) return;
            rules.Add(new BoardRule { Label = label, Kind = "trunk", Patterns = patterns.Distinct(StringComparer.Ordinal).ToArray() });
            label = null; patterns.Clear();
        }
        foreach (Match match in Regex.Matches(body, "writer\\.WriteLine\\(\\$?\"([^\"]*)\"\\);", RegexOptions.CultureInvariant))
        {
            var value = match.Groups[1].Value;
            if (value.StartsWith('[')) Finish();
            else if (value.StartsWith("label=", StringComparison.Ordinal)) label = value[6..];
            else if (value.StartsWith("channel=", StringComparison.Ordinal))
            {
                var key = value[8..];
                // These two literal fallback stems in the supplied FOP list are
                // made explicit in the new engine. Do not wildcard all SIP names.
                patterns.Add(key is "SIP/in-" or "SIP/out" ? key + "*" : key);
            }
        }
        Finish();
        var result = new BoardConfiguration { Rules = rules.ToArray() };
        result.Validate();
        if (result.Rules.Length == 0) throw new InvalidOperationException("No trunk groups found.");
        return result;
    }

    public static BoardConfiguration Merge(BoardConfiguration current, BoardConfiguration catalog)
    {
        current.Validate(); catalog.Validate();
        var rules = current.Rules.ToList();
        foreach (var rule in catalog.Rules)
        {
            var existing = rules.FirstOrDefault(r => r.Id == rule.Id ||
                (string.Equals(r.Label, rule.Label, StringComparison.OrdinalIgnoreCase) && r.Node == rule.Node));
            if (existing is not null)
            {
                if (existing.Kind != rule.Kind || existing.Enabled != rule.Enabled ||
                    !existing.Patterns.SequenceEqual(rule.Patterns, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Existing rule conflicts with catalog: {rule.Label}. No changes applied.");
                continue;
            }
            rules.Add(rule);
        }
        var merged = current with { Rules = rules.ToArray() };
        merged.Validate();
        return merged;
    }
}
