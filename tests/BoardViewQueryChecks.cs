using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

internal static class BoardViewQueryChecks
{
    public static void Run(Action<bool, string> check)
    {
        var defaults = BoardViewQuery.Parse("https://panel.example/mesa");
        check(defaults.TextKinds.Length == 3 && !defaults.Channels, "URL defaults filter all kinds");
        var query = BoardViewQuery.Parse("https://panel.example/mesa?q=Hugo&node=eveo-voip&state=registered&text=queues,trunks&channels=true&company=11111111111171118111111111111111&page=4");
        check(query.Text == "Hugo" && query.Node == "eveo-voip" && query.State == "registered" && query.Channels, "URL filters restored");
        check(query.ContextId.HasValue && query.Page == 4 && query.TextKinds.SequenceEqual(new[] { "queues", "trunks" }), "Legacy URL company and selected kinds");
        check(query.Parameters()["contextid"]?.ToString() == "11111111111171118111111111111111" && query.Parameters()["company"] is null, "Write canonical contextid and remove legacy company");
        var canonical = BoardViewQuery.Parse("https://panel.example/board?contextid=22222222-2222-7222-8222-222222222222&company=11111111111171118111111111111111");
        check(canonical.ContextId == Guid.Parse("22222222222272228222222222222222"), "Canonical context wins over legacy company");
        check(BoardViewQuery.Parse("https://panel.example/board?contextid=bad&company=11111111111171118111111111111111").ContextId is null, "Invalid canonical context never falls back to different company");
        check(BoardViewQuery.Parse("https://panel.example/board?contextid=11111111111171118111111111111111&contextid=22222222222272228222222222222222").ContextId is null, "Ambiguous duplicate contexts rejected");
        check(BoardViewQuery.Parse("https://panel.example/board?contextid=00000000000000000000000000000000").ContextId is null, "Empty context rejected");
        check(BoardViewQuery.Parse("https://panel.example/?text=none").TextKinds.Length == 0, "Explicit no text targets");
        check(BoardViewQuery.Parse("https://panel.example/?text=invalid&state=secret&company=bad&page=-9").State == "all", "Invalid filters safe defaults");
        check(BoardViewQuery.Parse("https://panel.example/?q=" + new string('x', 300)).Text.Length == 100, "URL text bounded");
        check(defaults.Parameters().Values.All(v => v is null), "Defaults omitted from shared URL");
        check(PanelReturnUrl.Validate("/mesa?q=Hugo&text=peers", "") == "/mesa?q=Hugo&text=peers", "Login returns to shared filters");
        foreach (var url in new[] { "//evil.test", "/\\evil.test", "https://evil.test", "/api/fleet", "/mesa\n" })
            check(PanelReturnUrl.Validate(url, "") == "/", "Reject external or unsafe login return");
    }
}
