using System.Text.Json;
using Sufficit.Telephony.Panel.Localization;
using Sufficit.Telephony.Panel.Security;

static class PanelTextChecks
{
    public static void Run(Action<bool, string> check)
    {
        var first = new PanelText(); var second = new PanelText();
        check(first.Language == "en" && first["Full screen"] == "Full screen", "English is the default");
        first.SetLanguage("pt-BR");
        check(first["Full screen"] == "Tela cheia", "Portuguese is available");
        check(second.Language == "en", "Language is isolated per circuit");
        check(first.Format("{0} observed channels", 3) == "3 canais observados", "Parameterized Portuguese message");
        first.SetLanguage("invalid");
        check(first.Language == "en", "Unknown language falls back to English");
        check(first.Observed("Registrado no evento") == "Registered in event", "Legacy status translated without changing evidence");
        check(first.Observed("2 canal(is) observado(s)") == "2 observed channels", "Legacy channel count translated");
        check(first.Observed("PJSIP/6007") == "PJSIP/6007", "Identifiers are not translated");
        check(PanelReturnUrl.Validate("/board?q=6007&text=peers", "") == "/board?q=6007&text=peers", "English route is an allowed login return");
        using var stream = typeof(PanelText).Assembly.GetManifestResourceStream("Sufficit.Telephony.Panel.Localization.Portuguese.json")!;
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        foreach (var pair in entries)
        {
            var english = System.Text.CompositeFormat.Parse(pair.Key);
            var portuguese = System.Text.CompositeFormat.Parse(pair.Value);
            check(english.MinimumArgumentCount == portuguese.MinimumArgumentCount, "Translation placeholders match: " + pair.Key);
        }
    }
}
