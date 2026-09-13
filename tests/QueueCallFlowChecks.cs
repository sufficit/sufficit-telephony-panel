using System.Text.Json;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Panel.Operations;

internal static class QueueCallFlowChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var projection = new LiveProjection();
        void Send(string type, object fields, string node = "google") => projection.Apply(node, type, JsonSerializer.SerializeToElement(fields), now);
        ObservedResource[] Read() => projection.Snapshot(now);
        var channel = "PJSIP/0000006007-000000ab";
        Send("NewChannelEvent", new { channel, calleridnum = "0000006007", connectedlinenum = "<unknown>", exten = "6500", accountcode = context.ToString("N"), uniqueid = "1", linkedid = "1" });
        Send("NewStateEvent", new { channel, calleridnum = "", connectedlinenum = "<unknown>", exten = "", accountcode = "", linkedid = "", channelstate = "Up" });
        check(Read().Single().Caller == "0000006007" && Read().Single().Destination == "6500", "Empty state fields do not erase caller/destination");
        check(Read().Single().Details["linkedid"] == "1", "Empty Linkedid does not erase correlation");
        Send("NewChannelEvent", new { channel = "Local/6500@test-0001;1", calleridnum = "0000006007", accountcode = context.ToString("N"), linkedid = "1", uniqueid = "2" });
        Send("QueueCallerJoinEvent", new { channel, queue = "6500", calleridnum = "0000006007", connectedlinenum = "<unknown>", accountcode = "" });
        var waiting = Read().Single(r => r.Kind == "waiting");
        check(waiting.Account == context.ToString("N") && waiting.Details["linkedid"] == "1", "Queue join inherits exact-channel tenant and linked ID");
        var peer = new EventsPanelCardInfo { Label = "Hugo", Kind = EventsPanelCardKind.PEER, Channels = ["^PJSIP/0000006007"] };
        var queue = new EventsPanelCardInfo { Label = "Atendimento", Kind = EventsPanelCardKind.QUEUE, Channels = ["^Local/6500"] };
        var observation = AuthorizedObservation.Create([peer, queue], Read(), context);
        check(observation.ByCard[queue].Any(r => r.Kind == "waiting"), "Native queue ID matches authorized Local queue-card pattern");
        check(!LiveProjection.Matches(peer, waiting), "Queue normalization cannot match a peer card");
        var tiles = BoardCallFlow.Attach(OperatorTile.Create([peer, queue], observation.ByCard), observation.Rows);
        var hugo = tiles.Single(t => t.Title == "Hugo"); var atendimento = tiles.Single(t => t.Title == "Atendimento");
        check(hugo.Flows.Length == 1 && hugo.Flows[0].Destination == "Atendimento", "6007 displays one queue call with a meaningful destination");
        check(hugo.DetailChannels.Length == 2, "Related Local leg retained in channel diagnostics");
        check(atendimento.QueueCalls.Length == 1 && atendimento.Status.Tone == "busy", "Queue tile reflects observed waiting caller");
        check(AuthorizedObservation.Create([peer, queue], Read(), Guid.NewGuid()).Rows.Length == 0, "Queue identity cannot bypass tenant AccountCode");
        Send("QueueCallerJoinEvent", new { channel, queue = "6500" }, "eveo");
        check(string.IsNullOrEmpty(Read().Single(r => r.Kind == "waiting" && r.Node == "eveo").Account), "Same channel name on another node cannot inherit tenant");
        Send("QueueCallerLeaveEvent", new { channel, queue = "6500" });
        check(!Read().Any(r => r.Kind == "waiting" && r.Node == "google"), "Queue leave clears waiting before agent connection");
        Send("AgentConnectEvent", new { channel, queue = "6500", linkedid = "1" });
        check(Read().Single(r => r.Kind == "waiting" && r.Node == "google").State == "answered", "AgentConnect retains queue attribution through conversation");
        Send("QueueCallerLeaveEvent", new { channel, queue = "6500" });
        check(Read().Any(r => r.Kind == "waiting" && r.Node == "google"), "Late leave cannot erase answered queue call");
        Send("AgentCompleteEvent", new { channel, queue = "6500" });
        check(!Read().Any(r => r.Kind == "waiting" && r.Node == "google"), "AgentComplete clears queue activity");
        Send("QueueCallerJoinEvent", new { channel, queue = "6500" });
        Send("QueueCallerAbandonEvent", new { channel, queue = "6500" });
        check(!Read().Any(r => r.Kind == "waiting" && r.Node == "google"), "Abandon clears queue activity");
        Send("NewConnectedLine", new { channel, connectedlinenum = "6004", channelstate = "0" });
        check(Read().Single(r => r.Key == channel && r.Kind == "call").Destination == "6004", "Typed connected-line event updates destination");
        Send("QueueCallerJoinEvent", new { channel, queue = "6500" });
        Send("HangupEvent", new { channel });
        check(!Read().Any(r => r.Key == channel && r.Node == "google"), "Hangup removes channel and queue association");
    }
}
