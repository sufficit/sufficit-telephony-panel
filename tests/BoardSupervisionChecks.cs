using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Monitor;

public static class BoardSupervisionChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.NewGuid();
        var rule = new SupervisionPrefixRule { Id = "test-prefix", ContextId = context, Node = "test-node", CardKey = "PJSIP/6007", Prefix = "PJSIP/6007-" };
        var policy = new SupervisionPrefixPolicy([rule]);
        var tile = new OperatorTile("test", "Test", rule.Node, EventsPanelCardKind.PEER, []) { Keys = [rule.CardKey] };
        var row = new ObservedResource("channel", rule.Node, "call", "PJSIP/6007-00000001", "Up", "6007", "6500", context.ToString("N"), now, now, new Dictionary<string, string>());
        check(policy.For(tile, context).Single() == rule, "Protected prefix is bound to exact node, context and card key");
        check(policy.For(tile with { Node = "another-node" }, context).Length == 0, "Prefix cannot cross nodes");
        check(policy.For(tile, Guid.NewGuid()).Length == 0, "Prefix cannot cross selected contexts");
        check(policy.For(tile with { Keys = ["PJSIP/*"] }, context).Length == 0, "Display wildcard does not authorize prefix");
        check(SupervisionPrefixPolicy.Authorized(rule, [new() { Channels = [rule.CardKey] }]), "Fresh API card authorizes configured mapping");
        check(!SupervisionPrefixPolicy.Authorized(rule, [new() { Channels = [".*"] }]), "Broad API regex is not substituted for configured card identity");
        check(SupervisionPrefixPolicy.Observed(rule, [row], true, now), "Fresh observed target permits configured prefix");
        check(!SupervisionPrefixPolicy.Observed(rule, [row], false, now), "Disconnected stream rejects prefix");
        check(!SupervisionPrefixPolicy.Observed(rule, [row], true, now.AddSeconds(31)), "Stale prefix rejected");
        check(!SupervisionPrefixPolicy.Observed(rule, [row], true, now.AddSeconds(-1)), "Future event rejected");
        check(!SupervisionPrefixPolicy.Observed(rule, [row with { Key = "PJSIP/60070-00000001" }], true, now), "6007 prefix cannot match 60070");
        check(!SupervisionPrefixPolicy.Observed(rule, [row, row with { Account = Guid.NewGuid().ToString("N") }], true, now), "Conflicting tenant on shared trunk denies continuous supervision");
        foreach (var prefix in new[] { "PJSIP/", "PJSIP/6007", "PJSIP/.*", "", "PJSIP/6007-,B", "PJSIP/6007-\r\nAction: Hangup" })
        {
            try { _ = new SupervisionPrefixPolicy([rule with { Prefix = prefix }]); check(false, "Unsafe prefix must fail"); }
            catch (InvalidOperationException) { check(true, "Unsafe prefix rejected without an AMI request"); }
        }
        var queue = tile with { Kind = EventsPanelCardKind.QUEUE, Keys = ["queue-business-id"] };
        check(policy.For(queue, context).Length == 0, "Queue name never inferred as a channel prefix");
        check(new SupervisionPrefixPolicy([]).For(tile, context).Length == 0, "Prefix default is opt-in");
        var endpointTile = tile with { Rows = [row] };
        var endpoint = EndpointSupervisionTarget.For(endpointTile, context, true, now).Single();
        check(endpoint.Prefix == "PJSIP/6007-", "Endpoint prefix keeps the channel delimiter");
        check(endpoint.Authorized([new() { Kind = EventsPanelCardKind.PEER, Channels = [rule.CardKey] }]), "Concrete scoped API endpoint authorizes automatic prefix");
        check(!endpoint.Authorized([new() { Kind = EventsPanelCardKind.TRUNK, Channels = [rule.CardKey] }]), "Shared trunk cannot impersonate endpoint authorization");
        check(!endpoint.Authorized([new() { Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/.*"] }]), "Regex card cannot authorize inferred endpoint");
        check(EndpointSupervisionTarget.For(endpointTile with { Kind = EventsPanelCardKind.QUEUE }, context, true, now).Length == 0, "Single queue channel does not authorize continuous routing");
        check(EndpointSupervisionTarget.For(endpointTile with { Kind = EventsPanelCardKind.TRUNK }, context, true, now).Length == 0, "Single trunk channel does not authorize future calls");
        check(EndpointSupervisionTarget.For(endpointTile, Guid.NewGuid(), true, now).Length == 0, "Automatic prefix cannot cross selected context");
        check(EndpointSupervisionTarget.For(endpointTile, context, false, now).Length == 0, "Disconnected endpoint cannot initiate prefix listening");
        check(EndpointSupervisionTarget.For(endpointTile, context, true, now.AddSeconds(31)).Length == 0, "Stale endpoint cannot initiate prefix listening");
        check(EndpointSupervisionTarget.For(endpointTile with { Rows = [row with { Account = null }] }, null, true, now).Length == 0, "Unknown context cannot authorize endpoint prefix");
        check(EndpointSupervisionTarget.For(endpointTile with { Keys = ["PJSIP/600"] }, context, true, now).Length == 0, "Endpoint keys require exact equality");
        var sip = endpointTile with { Keys = ["SIP/0000006001"], Rows = [row with { Key = "SIP/0000006001-00000001" }] };
        check(EndpointSupervisionTarget.For(sip, null, true, now).Single().Prefix == "SIP/0000006001-", "Reported SIP extension resolves its own precise prefix");
        var anchored = EndpointSupervisionTarget.For(sip with { Keys = ["^SIP/0000006001"] }, null, true, now).Single();
        check(anchored.Authorized([new() { Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0000006001"] }]), "Portal anchored endpoint pattern resolves by literal identity");
        check(!anchored.Authorized([new() { Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0000006001.*"] }]), "Anchored wildcard is not an endpoint permission");
        check(EndpointSupervisionTarget.For(endpointTile with { Rows = [], Flows = [new BoardCallFlow("flow", "6007", "6500", "Up", true, [row])] }, context, true, now).Length == 0, "Related legs cannot grant endpoint supervision");
        var idlePeer = row with { Kind = "peer", Key = "SIP/0000006007", Account = null, Updated = now.AddHours(-2), State = "Registered" };
        var idleTile = tile with { Rows = [idlePeer], Keys = ["^SIP/0000006007"] };
        var idleTarget = EndpointSupervisionTarget.For(idleTile, context, true, now).Single();
        check(idleTarget.Prefix == "SIP/0000006007-", "Idle peer without channel resolves in explicitly scoped context");
        check(idleTarget.Observed([idlePeer], true, now), "Stable peer state is not expired by the channel age limit");
        check(!idleTarget.Observed([idlePeer], false, now), "Disconnected telemetry cannot authorize idle peer");
        check(!idleTarget.Observed([idlePeer with { Account = Guid.NewGuid().ToString("N") }], true, now), "Peer's explicit foreign account rejects selected context");
        check(!idleTarget.Observed([idlePeer, row with { Key = "SIP/0000006007-00000001", Account = Guid.NewGuid().ToString("N") }], true, now), "Conflicting active channel rejects idle prefix");
        check(!idleTarget.Observed([idlePeer with { Updated = now.AddMinutes(1) }], true, now), "Future peer timestamp is rejected");
        check(EndpointSupervisionTarget.For(idleTile, null, true, now).Length == 0, "Unknown idle context is not guessed from peer number");
        check(EndpointSupervisionTarget.For(idleTile with { Rows = [] }, context, true, now).Length == 0, "Remembered-only peer cannot authorize supervision");
        check(EndpointSupervisionTarget.For(idleTile with { Rows = [idlePeer with { Account = context.ToString("N") }] }, null, true, now).Length == 1, "Explicit peer account supports unfiltered manager view");
        check(TelephonyMonitorOptions.Build(TelephonyMonitorActionMode.Listen, true) == "quES", "Exact listen uses unique channel and exits on hangup or no candidate");
        check(TelephonyMonitorOptions.Build(TelephonyMonitorActionMode.Whisper, true) == "quESw", "Exact whisper targets one leg without barge");
        check(TelephonyMonitorOptions.Build(TelephonyMonitorActionMode.Listen, false) == "q", "Continuous listen waits for subsequent prefix channels");
        check(TelephonyMonitorOptions.Build(TelephonyMonitorActionMode.Whisper, false) == "qw", "Continuous whisper does not silently enable two-party barge");
        check(!TelephonyMonitorOptions.Build(TelephonyMonitorActionMode.Listen, true).Contains('b'), "Waiting caller and greeting audio are not excluded by a bridged-only flag");
        try { TelephonyMonitorOptions.Build((TelephonyMonitorActionMode)100, true); check(false, "Invalid mode must fail"); }
        catch (ArgumentOutOfRangeException) { check(true, "Invalid mode rejected"); }
    }
}
