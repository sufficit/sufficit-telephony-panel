using System.Text.RegularExpressions;
using Sufficit.Telephony.Panel.Operations;

static class CardMatcherChecks
{
    public static void Run(Action<bool, string> check)
    {
        var patterns = new[] { "^SIP/0000286013", "SIP/6001", "^PJSIP/6007$", "trunk-demo$", "^Local/6599", "IAX2/143.208.224.21:26497",
            "^SIP/00002860(1|2)", "^PJSIP/wss-[0-9a-f]+", "SIP/in-dtr-67", "^IAX2/gateway-phonevox" };
        var candidates = new[] { "SIP/0000286013", "SIP/0000286013-000000ab", "SIP/00002860130", "XSIP/0000286013", "SIP/6001", "PJSIP/SIP/6001x",
            "PJSIP/6007", "PJSIP/6007-0001", "PJSIP/trunk-demo", "PJSIP/trunk-demo-1", "Local/6599@queue-0001;1", "IAX2/143.208.224.21:26497-8551",
            "IAX2/143x208.224.21:26497", "SIP/0000286021", "PJSIP/wss-71a36cbccb2511e", "SIP/in-dtr-67", "IAX2/gateway-phonevox", "" };
        var mismatches = 0;
        foreach (var pattern in patterns)
        {
            var matcher = new CardMatcher([pattern]);
            var regex = new Regex(pattern, RegexOptions.CultureInvariant);
            foreach (var candidate in candidates)
            {
                // Legacy literal shortcut (equality or "pattern-") was always part of the contract.
                var expected = regex.IsMatch(candidate) || candidate == pattern || candidate.StartsWith(pattern + "-", StringComparison.Ordinal);
                if (matcher.Matches(candidate) != expected) { mismatches++; Console.WriteLine($"CardMatcher mismatch: {pattern} vs {candidate}"); }
            }
        }
        check(mismatches == 0, "Card matching keeps regex semantics for literal fast paths and compiled patterns");
        check(!new CardMatcher(["^SIP/(unclosed"]).Matches("SIP/(unclosed"), "Invalid pattern never matches");
        // Thousands of fresh card instances (central view refresh) must not keep allocating regex automata.
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 2000; i++) new CardMatcher(["^SIP/" + (1000000 + i % 50)]).Matches("SIP/1000001");
        check(GC.GetAllocatedBytesForCurrentThread() - before < 8 * 1024 * 1024, "Anchored literal cards build no regex automata");
    }
}
