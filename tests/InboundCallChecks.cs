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
