using System.Globalization;
using System.Text.Json;

namespace Sufficit.Telephony.Panel.Localization;

/// <summary>Per-circuit UI language. Never changes process-wide or telemetry culture.</summary>
public sealed class PanelText
{
    private static readonly IReadOnlyDictionary<string, string> Portuguese = Load();
    private static readonly IReadOnlyDictionary<string, string> English = Portuguese
        .GroupBy(p => p.Value, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);
    public string Language { get; private set; } = "en";
    public CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "pt-BR" ? "pt-BR" : "en-US");
    public event Action? Changed;
    public string this[string english] => Language == "pt-BR" ? Portuguese.GetValueOrDefault(english, english) : english;
    public string Format(string english, params object?[] values) => string.Format(Culture, this[english], values);
    /// <summary>Translate known legacy presentation labels, never identifiers or customer names.</summary>
    public string Observed(string value)
    {
        if (English.TryGetValue(value, out var english)) return this[english];
        const string suffix = " canal(is) observado(s)";
        if (value.EndsWith(suffix, StringComparison.Ordinal) && int.TryParse(value[..^suffix.Length], out var count))
            return Format("{0} observed channels", count);
        return this[value];
    }
    public void SetLanguage(string? language)
    {
        var next = language == "pt-BR" ? "pt-BR" : "en";
        if (next == Language) return;
        Language = next;
        Changed?.Invoke();
    }
    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = typeof(PanelText).Assembly.GetManifestResourceStream("Sufficit.Telephony.Panel.Localization.Portuguese.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
