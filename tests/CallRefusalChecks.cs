using System.Text.Json;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Streaming;

/// <summary>"Do not disturb" on a phone: immediate busy (cause 17) while the extension has no other call.</summary>
internal static class CallRefusalChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Evidence a few seconds in the past, as it reaches the board from the event stream.
        var now = DateTimeOffset.UtcNow.AddSeconds(-10);
        var tenant = Guid.NewGuid();
        JsonElement Channel(string key, string state = "Down") => JsonSerializer.SerializeToElement(new
        { channel = key, channelstate = state, accountcode = tenant.ToString("N"), linkedid = "private-call", calleridnum = "private-caller" });
        JsonElement Hangup(string key, int cause) => JsonSerializer.SerializeToElement(new
        { channel = key, cause, accountcode = tenant.ToString("N"), linkedid = "private-call" });
        ObservedResource? Refusal(LiveProjection p, DateTimeOffset at) => p.Snapshot(at).SingleOrDefault(r => r.Kind == RecentCallRefusal.Kind);

        var store = new TelemetryStore();
        for (var i = 0; i < 3; i++)
        {
            var key = $"SIP/0006676007-0000000{i}";
            store.Append("google", "NewChannelEvent", Channel(key), now.AddSeconds(i));
            store.Append("google", "HangupEvent", Hangup(key, 17), now.AddSeconds(i).AddMilliseconds(200));
        }
        var replica = new TelemetryReplica();
        foreach (var frame in store.Read("", 0).Frames) replica.Apply(frame);
        var rows = replica.Projection.Snapshot(now.AddSeconds(3));
        var refusal = rows.Single(r => r.Kind == RecentCallRefusal.Kind);
        check(refusal.Key == "SIP/0006676007" && refusal.Account == tenant.ToString("N"), "Refusal keeps the exact endpoint and tenant");
        check(refusal.Details.GetValueOrDefault("count") == "3" && refusal.Details.GetValueOrDefault("cause") == "17", "Repeated refusals are counted, not duplicated");
        check(refusal.Caller is null or "" && !refusal.Details.ContainsKey("linkedid"), "Refusal does not retain caller or call metadata");
        var observation = AuthorizedObservation.ForManager(rows);
        var tile = OperatorTile.Create(observation.Cards, observation.ByCard).Single();
        check(tile.RefusingCalls is not null && tile.Status.Tone != "refusing", "Extension card shows likely Do not disturb without replacing the evidence tone");
        check(BoardTotals.ActiveCalls([tile]) == 0, "Refusal cannot inflate active-call counters");
        var card = new EventsPanelCardInfo { Label = "agent", Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0006676007"] };
        check(AuthorizedObservation.Create([card], rows, tenant).Rows.Any(r => r.Kind == RecentCallRefusal.Kind), "Owner tenant sees the refusal");
        check(!AuthorizedObservation.Create([card], rows, Guid.NewGuid()).Rows.Any(r => r.Kind == RecentCallRefusal.Kind), "Another tenant cannot see the refusal");
        check(!AuthorizedObservation.Create([card], rows.Select(r => r with { Account = null }), tenant).Rows.Any(r => r.Kind == RecentCallRefusal.Kind), "Unowned refusal stays out of customer views");
        check(replica.Projection.Snapshot(now.AddSeconds(3) + RecentCallRefusal.Lifetime).All(r => r.Kind != RecentCallRefusal.Kind), "Refusal fades after its lifetime without new evidence");

        // Busy because the extension is on another call is ordinary busy, not Do not disturb.
        var inCall = new LiveProjection();
        inCall.Apply("google", "NewChannelEvent", Channel("SIP/0006676008-000000a1", "Up"), now);
        inCall.Apply("google", "NewChannelEvent", Channel("SIP/0006676008-000000a2"), now);
        inCall.Apply("google", "HangupEvent", Hangup("SIP/0006676008-000000a2", 17), now.AddMilliseconds(100));
        check(Refusal(inCall, now) is null, "Busy during another call of the same extension is not a refusal");

        // A person declining after ringing, or a slow busy, is not a device setting.
        var declined = new LiveProjection();
        declined.Apply("google", "NewChannelEvent", Channel("SIP/0006676009-000000b1"), now);
        declined.Apply("google", "NewStateEvent", Channel("SIP/0006676009-000000b1", "Ringing"), now.AddMilliseconds(500));
        declined.Apply("google", "HangupEvent", Hangup("SIP/0006676009-000000b1", 17), now.AddSeconds(1));
        check(Refusal(declined, now.AddSeconds(1)) is null, "Busy after ringing (declined by a person) is not a refusal");
        var slow = new LiveProjection();
        slow.Apply("google", "NewChannelEvent", Channel("SIP/0006676009-000000b2"), now);
        slow.Apply("google", "HangupEvent", Hangup("SIP/0006676009-000000b2", 17), now + RecentCallRefusal.ImmediateWindow + TimeSpan.FromSeconds(1));
        check(Refusal(slow, now.AddSeconds(5)) is null, "Busy after the immediate window is not a refusal");
        foreach (var cause in new[] { 16, 18, 19, 21, 1 })
        {
            var other = new LiveProjection();
            other.Apply("google", "NewChannelEvent", Channel("SIP/0006676005-000000c1"), now);
            other.Apply("google", "HangupEvent", Hangup("SIP/0006676005-000000c1", cause), now.AddMilliseconds(100));
            check(Refusal(other, now) is null, "Only busy (17) indicates a refusal: cause " + cause);
        }
        var trunk = new LiveProjection();
        trunk.Apply("google", "NewChannelEvent", Channel("Local/0006676005@queue-000000d1;2"), now);
        trunk.Apply("google", "HangupEvent", Hangup("Local/0006676005@queue-000000d1;2", 17), now.AddMilliseconds(100));
        check(Refusal(trunk, now) is null, "Technical Local legs never create refusals");

        // Ringing for an incoming call proves the phone accepts calls again.
        var cleared = new LiveProjection();
        cleared.Apply("google", "NewChannelEvent", Channel("SIP/0006676006-000000e1"), now);
        cleared.Apply("google", "HangupEvent", Hangup("SIP/0006676006-000000e1", 17), now.AddMilliseconds(100));
        check(Refusal(cleared, now) is not null, "Refusal recorded before clearing");
        cleared.Apply("other", "NewStateEvent", Channel("SIP/0006676006-000000e2", "Ringing"), now.AddSeconds(1));
        check(Refusal(cleared, now.AddSeconds(1)) is not null, "Ringing on another node does not clear this node's refusal");
        cleared.Apply("google", "NewChannelEvent", Channel("SIP/0006676006-000000e3"), now.AddSeconds(2));
        check(Refusal(cleared, now.AddSeconds(2)) is not null, "A new attempt alone does not clear the refusal");
        cleared.Apply("google", "NewStateEvent", Channel("SIP/0006676006-000000e3", "Ringing"), now.AddSeconds(2));
        check(Refusal(cleared, now.AddSeconds(2)) is null, "Ringing on the extension clears the refusal");

        // An active call on the card takes precedence over the refusal badge.
        var active = OperatorTile.Create(observation.Cards, observation.ByCard.ToDictionary(p => p.Key,
            p => p.Value.Append(new ObservedResource("google|call|SIP/0006676007-000000f1", "google", "call", "SIP/0006676007-000000f1", "Up",
                null, null, tenant.ToString("N"), now, now, new Dictionary<string, string>())).ToList())).Single();
        check(active.RefusingCalls is null, "A live call hides the refusal badge");

        // Do not disturb confirmed by the PBX device state (FreePBX Custom:DND), distinct from a likely refusal.
        JsonElement Dnd(string device, string state) => JsonSerializer.SerializeToElement(new { device, state });
        var pbx = new LiveProjection();
        pbx.Apply("google", "DeviceStateChangeEvent", Dnd("Custom:DND0006676031", "BUSY"), now);
        var dndRow = pbx.Snapshot(now).Single();
        check(dndRow.Kind == ConfirmedDoNotDisturb.Kind && dndRow.Key == "0006676031", "PBX Do not disturb becomes one row per extension");
        var sipCard = new EventsPanelCardInfo { Label = "agent", Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0006676031"] };
        var otherCard = new EventsPanelCardInfo { Label = "other", Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0006676032"] };
        var authorized = AuthorizedObservation.Create([sipCard, otherCard], pbx.Snapshot(now), tenant);
        check(authorized.ByCard.TryGetValue(sipCard, out var own) && own.Any(r => r.Kind == ConfirmedDoNotDisturb.Kind)
            && !authorized.ByCard.ContainsKey(otherCard), "PBX Do not disturb attaches only to the exact extension card");
        var confirmedTile = OperatorTile.Create(authorized.Cards, authorized.ByCard).Single(t => t.Title == "agent");
        check(confirmedTile.DndLevel == "confirmed", "PBX Do not disturb is confirmed, not likely");
        check(tile.DndLevel == "likely", "Refusals alone stay likely");
        pbx.Apply("google", "DeviceStateChangeEvent", Dnd("Custom:DND0006676031", "NOT_INUSE"), now.AddSeconds(5));
        check(pbx.Snapshot(now.AddSeconds(5)).Length == 0, "Turning Do not disturb off removes the confirmation");
        pbx.Apply("google", "DeviceStateChangeEvent", Dnd("Custom:DND0006676031", "INUSE"), now.AddSeconds(6));
        check(pbx.Snapshot(now.AddHours(3)).Length == 1, "Confirmed Do not disturb does not fade like transient evidence");
        var plain = new LiveProjection();
        plain.Apply("google", "DeviceStateChangeEvent", Dnd("SIP/0006676031", "BUSY"), now);
        check(plain.Snapshot(now).All(r => r.Kind != ConfirmedDoNotDisturb.Kind), "Ordinary device busy is not Do not disturb");

        // Native presence keyed by the extension GUID (CustomPresence:<guid>), attached to the PJSIP/<guid> card.
        const string guid = "71a36cbccb2511e8af50001c42ead1e6";
        JsonElement Presence(string status, string? subtype = null, string? message = null)
            => JsonSerializer.SerializeToElement(new { presentity = "CustomPresence:" + guid, status, subtype, message });
        var presence = new LiveProjection();
        presence.Apply("google", "PresenceStateChangeEvent", Presence("away", "busy", "Almoço, volto 14h"), now);
        var presenceRow = presence.Snapshot(now).Single();
        check(presenceRow.Kind == ExtensionPresenceState.Kind && presenceRow.Key == guid && presenceRow.State == "away"
            && presenceRow.Details["subtype"] == "busy" && presenceRow.Details["message"] == "Almoço, volto 14h", "Presence row keeps state, subtype and note by GUID");
        var pjsipCard = new EventsPanelCardInfo { Label = "pj", Kind = EventsPanelCardKind.PEER, Channels = ["^PJSIP/" + guid] };
        var numberCard = new EventsPanelCardInfo { Label = "num", Kind = EventsPanelCardKind.PEER, Channels = ["^SIP/0006676031"] };
        var presenceView = AuthorizedObservation.Create([pjsipCard, numberCard], presence.Snapshot(now), tenant);
        check(presenceView.ByCard.ContainsKey(pjsipCard) && !presenceView.ByCard.ContainsKey(numberCard), "Presence attaches only to the PJSIP/<guid> card, never by number");
        var busyTile = OperatorTile.Create(presenceView.Cards, presenceView.ByCard).Single(t => t.Title == "pj");
        check(busyTile.PresenceStatus is { Label: "Busy", Message: "Almoço, volto 14h" } && busyTile.DndLevel is null, "Away with subtype busy is Busy with its note, not DND");
        presence.Apply("google", "PresenceStateChangeEvent", Presence("dnd"), now.AddSeconds(1));
        var dndView = AuthorizedObservation.Create([pjsipCard], presence.Snapshot(now.AddSeconds(1)), tenant);
        check(OperatorTile.Create(dndView.Cards, dndView.ByCard).Single().DndLevel == "confirmed", "Presence dnd is confirmed Do not disturb");
        presence.Apply("google", "PresenceStateChangeEvent", Presence("not_set"), now.AddSeconds(2));
        check(presence.Snapshot(now.AddSeconds(2)).Length == 0, "not_set removes the presence");
        var foreign = new LiveProjection();
        foreign.Apply("google", "PresenceStateChangeEvent", JsonSerializer.SerializeToElement(new { presentity = "CustomPresence:6001", status = "dnd" }), now);
        check(foreign.Snapshot(now).Length == 0, "Only GUID-keyed presence is projected");

        // Manager view: presence (key = GUID) reaches the endpoint's card, never a standalone GUID card.
        {
            const string g = "71a36cbccb2511e8af50001c42ead1e6";
            var mgrPresence = new ObservedResource("google|presence|" + g, "google", ExtensionPresenceState.Kind, g, "dnd", null, null, null, now, now, new Dictionary<string, string>());
            var wssPeer = new ObservedResource("google|peer|PJSIP/wss-" + g, "google", "peer", "PJSIP/wss-" + g, "Registered", null, null, null, now, now, new Dictionary<string, string>());
            var withPeer = AuthorizedObservation.ForManager([mgrPresence, wssPeer]);
            var tiles = OperatorTile.Create(withPeer.Cards, withPeer.ByCard);
            check(tiles.Length == 1 && tiles[0].DndLevel == "confirmed", "Manager view attaches presence to the existing PJSIP/wss-<guid> card");
            var alone = AuthorizedObservation.ForManager([mgrPresence]);
            var aloneTiles = OperatorTile.Create(alone.Cards, alone.ByCard);
            check(aloneTiles.Length == 1 && aloneTiles[0].Keys.SequenceEqual(new[] { "PJSIP/wss-" + g }) && aloneTiles[0].DndLevel == "confirmed",
                "Without a card, presence gets the Chrome extension's PJSIP/wss-<guid> card");
            var desk = new ObservedResource("google|peer|PJSIP/" + g, "google", "peer", "PJSIP/" + g, "Registered", null, null, null, now, now, new Dictionary<string, string>());
            var withDesk = AuthorizedObservation.ForManager([mgrPresence, desk]);
            check(OperatorTile.Create(withDesk.Cards, withDesk.ByCard).Single().DndLevel == "confirmed", "A PJSIP/<guid> card takes the presence without creating a WebRTC card");
        }

        // Presence is written on every node: it must not split the extension into one tile per node.
        {
            const string g = "71a36cbccb2511e8af50001c42ead1e6";
            ObservedResource P(string node, int age) => new(node + "|presence|" + g, node, ExtensionPresenceState.Kind, g, "dnd", null, null, null, now.AddSeconds(-age), now.AddSeconds(-age), new Dictionary<string, string>());
            var onlyPresence = AuthorizedObservation.ForManager([P("google", 5), P("apoint", 1)]);
            var single = OperatorTile.Create(onlyPresence.Cards, onlyPresence.ByCard);
            check(single.Length == 1 && single[0].Node == "apoint" && single[0].DndLevel == "confirmed", "Presence from two nodes is one tile, on the node that reported it last");
            var reg = new ObservedResource("google|peer|PJSIP/wss-" + g, "google", "peer", "PJSIP/wss-" + g, "Registered", null, null, null, now, now, new Dictionary<string, string>());
            var withReg = AuthorizedObservation.ForManager([P("google", 5), P("apoint", 1), reg]);
            var regTiles = OperatorTile.Create(withReg.Cards, withReg.ByCard);
            check(regTiles.Length == 1 && regTiles[0].Node == "google" && regTiles[0].DndLevel == "confirmed"
                && regTiles[0].Rows.Count(r => r.Kind == ExtensionPresenceState.Kind) == 1, "Presence joins the node where the extension is registered, once");
        }
    }
}
