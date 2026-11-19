using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class InboundCallChecks
{
    public static void Run(Action<bool, string> check)
    {
        var context = Guid.NewGuid(); var did = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var card = new EventsPanelCardInfo { Kind = EventsPanelCardKind.TRUNK, Label = "DID · 557137170655",
            Channels = [$"entry:did:{context:N}:{did:N}:configured"] };
        var incoming = new InboundCallSnapshot(context, did, "google-voip.sufficit.com.br", "SIP/provider-00000001",
            "1795123456.42", "1795123456.42", "Up", "caller", "6200", now);
        var row = incoming.Row();
        check(row.Node == "google-voip", "Inbound source uses the observed node identity");
        var view = AuthorizedObservation.Create([card], [row], context);
        var tile = OperatorTile.Create(view.Cards, view.ByCard).Single();
        check(view.Rows.Length == 1 && tile.HasObservedActivity && tile.Status.Tone == "busy", "Proven DID call overrides catalogue-only status");
        check(BoardTotals.ActiveCalls([tile]) == 1, "DID header includes one incoming call");
        check(!InboundCatalogueEntry.Matches(card, row with { Account = Guid.NewGuid().ToString("N") }), "Foreign account cannot bind to DID");
        check(!InboundCatalogueEntry.Matches(card, (incoming with { DidId = Guid.NewGuid() }).Row()), "Another DID cannot bind by the same label or carrier");
        check(!InboundCatalogueEntry.Matches(card, row with { Details = new Dictionary<string,string> { ["didid"] = did.ToString("N") } }), "A dialed destination without verified source cannot bind to DID");
        var plain = row with { Details = new Dictionary<string,string> { ["uniqueid"] = incoming.UniqueId }, Destination = "557137170655" };
        check(!AuthorizedObservation.Create([card], [plain], context).Rows.Any(), "Calling the DID number does not itself prove inbound direction");
        check(InboundCallSnapshot.Merge([plain], [incoming]).Length == 1, "Same AMI channel and DID snapshot are not duplicated");
        var two = AuthorizedObservation.Create([card], [row, (incoming with { Node = "eveo-voip", Channel = "SIP/provider-00000002", UniqueId = "1795123456.43", LinkedId = "1795123456.43" }).Row()], context);
        var grouped = OperatorTile.Create(two.Cards, two.ByCard);
        check(grouped.Length == 1 && BoardTotals.ActiveCalls(grouped) == 2, "One DID card aggregates live channels across nodes");
        var endpoint = Guid.NewGuid();
        var receiving = incoming with { Channel = "PJSIP/0004756200-00000a92", UniqueId = "1795126417.571344", LinkedId = "1795126416.571333", ReceiverEndpointId = endpoint };
        var peer = new EventsPanelCardInfo { Kind = EventsPanelCardKind.PEER, Label = "6200", Channels = ["^SIP/0004756200", $"^PJSIP/{endpoint:N}"] };
        var receivingView = AuthorizedObservation.Create([card, peer], [receiving.Row()], context);
        var receivingTiles = OperatorTile.Create(receivingView.Cards, receivingView.ByCard);
        check(receivingTiles.All(t => BoardTotals.ActiveCalls([t]) == 1 && t.Status.Tone == "busy"), "Captured numeric PJSIP receiver appears on DID and extension with cached GUID-only catalogue");
        var nonAnchor = receiving with { DidAnchor = false };
        check(!InboundCatalogueEntry.Matches(card, nonAnchor.Row()) && InboundCallSnapshot.MatchesEndpoint(peer, nonAnchor.Row()), "Non-anchor receiver still belongs to extension but does not double-count DID");
        var endpointOnly = nonAnchor with { DidId = Guid.Empty };
        check(!InboundCatalogueEntry.Matches(card, endpointOnly.Row()) && InboundCallSnapshot.MatchesEndpoint(peer, endpointOnly.Row()), "A direct receiving endpoint call does not fabricate a DID association");
        var originAndReceiver = AuthorizedObservation.Create([card, peer], [incoming.Row(), nonAnchor.Row()], context);
        var split = OperatorTile.Create(originAndReceiver.Cards, originAndReceiver.ByCard);
        check(split.Single(t => t.Kind == EventsPanelCardKind.TRUNK).Channels.Length == 1 && split.Single(t => t.Kind == EventsPanelCardKind.PEER).Channels.Length == 1,
            "DID and receiving extension each count one channel when origin is also observed");
        var classified = BoardRuleEngine.Apply([tile], new BoardConfiguration { Rules = [new BoardRule { Id = "carrier", Label = "Shared carrier", Kind = "trunk", Patterns = ["SIP/provider*"] }] });
        check(classified.Single().Title == card.Label && classified.Single().Keys.SequenceEqual(card.Channels), "Carrier classification cannot consume customer DID identity");
        var ended = AuthorizedObservation.Create([card], [], context);
        tile = OperatorTile.Create(ended.Cards, ended.ByCard).Single();
        check(BoardTotals.ActiveCalls([tile]) == 0 && tile.Status.Tone == "unknown", "Completed call leaves DID header and returns catalogue status");
        var sources = new[] { new InboundCallSource("google-voip", true, now, []) };
        var idle = InboundCallSnapshot.IdleEvidence([card], sources, context, now);
        var current = AuthorizedObservation.Create([card], idle, context);
        tile = OperatorTile.Create(current.Cards, current.ByCard).Single();
        check(tile.Status.Label == "No incoming calls" && BoardTotals.ActiveCalls([tile]) == 0, "Healthy empty DID snapshot is explicit instead of claiming no realtime monitoring");
        check(InboundCallSnapshot.IdleEvidence([card], [sources[0] with { Available = false }], context, now).Length == 0, "Source failure cannot produce a confirmed idle status");
        check(incoming.Fresh(now) && !incoming.Fresh(now.AddSeconds(9)), "DID call snapshot has a bounded freshness window");
        check(!new InboundCallSource(incoming.Node, false, now, []).Fresh(now), "Unavailable source is not a confirmed idle snapshot");
    }
}
