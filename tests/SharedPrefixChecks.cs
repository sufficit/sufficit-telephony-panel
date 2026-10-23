using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class SharedPrefixChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        ObservedResource Call(string id, string key, string? account, string node = "google-voip") =>
            new(id, node, "call", key, "Up", "", "", account, now, now, new Dictionary<string, string>());
        check(SharedSupervisionPrefix.PrefixOf("SIP/IN-DATORA-000000a1") == "SIP/IN-DATORA-", "Trunk channel yields its trunk prefix");
        check(SharedSupervisionPrefix.PrefixOf("IAX2/gateway-phonevox-10359") == "IAX2/gateway-phonevox-", "IAX2 call number yields the trunk prefix");
        check(SharedSupervisionPrefix.PrefixOf("Local/6007@from-queue-0001;1") is null, "Local legs never become a shared prefix");
        check(SharedSupervisionPrefix.PrefixOf("SIP/IN-DATORA") is null, "A peer name without channel suffix is not a prefix");
        var trunk = new OperatorTile("t", "IN-DATORA", "google-voip", EventsPanelCardKind.TRUNK,
            [Call("a", "SIP/IN-DATORA-000000a1", Guid.NewGuid().ToString("N")), Call("b", "SIP/IN-DATORA-000000b2", null), Call("c", "SIP/IN-DATORA-000000c3", null, "eveo-voip")]);
        check(SharedSupervisionPrefix.For(trunk, true, true).SequenceEqual(["SIP/IN-DATORA-"]), "Managers get one prefix per trunk on the card's server");
        check(SharedSupervisionPrefix.For(trunk, false, true).Length == 0, "Customers never get a shared trunk prefix");
        check(SharedSupervisionPrefix.For(trunk, true, false).Length == 0, "A disconnected stream offers no shared prefix");
        check(SharedSupervisionPrefix.For(trunk with { Kind = EventsPanelCardKind.PEER }, true, true).Length == 0, "Extensions keep their exclusive endpoint prefix path");
        check(SharedSupervisionPrefix.Companies(trunk.Rows, "google-voip", "SIP/IN-DATORA-").Contains("unknown"), "Audit lists legs without company as unknown");
        check(SharedSupervisionPrefix.Companies(trunk.Rows, "google-voip", "SIP/IN-DATORA-").Length == 2, "Audit covers only the trunk's server");
    }
}
