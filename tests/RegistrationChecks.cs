using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

static class RegistrationChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.NewGuid(); var node = Guid.NewGuid();
        AcdContact[] contacts = [new("6001", "192.0.2.7", "2001:db8::1", "Synthetic Phone", now.AddMinutes(10))];
        var agent = new AcdAgentPresence(context.ToString("N"), "6001", now, true, true, false, contacts,
            [new(now, "observed", true, true, contacts)], now);
        var snapshot = new AcdBoardSnapshot(context.ToString("N"), node.ToString("N"), now, true, false, [], [], [agent]);
        bool Valid(AcdAgentPresence a) => AcdBoardReader.ValidSnapshot(snapshot with { Agents = [a] }, context, node);
        check(Valid(agent), "Contact IP/device/history accepted on matching scope");
        check(!Valid(agent with { ContextId = Guid.NewGuid().ToString("N") }), "Foreign diagnostic context rejected");
        check(!Valid(agent with { Contacts = [contacts[0] with { IpAddress = "sip:user:secret@host" }] }), "Raw URI is not an IP");
        check(!Valid(agent with { Contacts = [contacts[0] with { UserAgent = "bad\r\nagent" }] }), "Control characters rejected in device data");
        check(!Valid(agent with { RegistrationHistory = Enumerable.Repeat(agent.RegistrationHistory![0], 21).ToArray() }), "History bound enforced");
        check(!Valid(agent with { Contacts = Enumerable.Repeat(contacts[0], 17).ToArray() }), "Contact bound enforced");
        check(!AcdBoardReader.ValidSnapshot(snapshot with { Agents = [agent, agent] }, context, node), "Duplicate identities rejected");
        check(AcdBoardReader.ValidSnapshot(snapshot with { Agents = null }, context, node), "Older sources without history remain readable");
        check(agent.State(now, true) == "Available", "Current registered free agent is available");
        check(agent.State(now.AddSeconds(16), true) == "No current data", "Stale registration cannot appear available");
        check(agent.State(now, false) == "No current data", "Unavailable source cannot appear live");
        check((agent with { Registered = false }).State(now, true) == "Unregistered", "Explicit unregister is distinct");
        check(PanelReturnUrl.Validate("/queues?contextid=" + context.ToString("N"), "").StartsWith("/queues?"), "Queue filters survive sign-in return validation");
        check(BoardViewQuery.Parse("https://panel.test/?tab=events").Tab == "events", "Event diagnostic tab survives URL reload");
        var unavailable = new AcdBoardSample("apoint-voip", context, null, false, node);
        check(unavailable.MatchesNode(node.ToString("D")) && unavailable.MatchesNode(node.ToString("N")), "Unavailable source retains configured GUID identity");
        check(!unavailable.MatchesNode(Guid.NewGuid().ToString("N")), "Unavailable source does not match other nodes");
    }
}
