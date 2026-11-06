using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

internal static class InboundCatalogueChecks
{
    public static void Run(Action<bool, string> check)
    {
        var own = Guid.NewGuid(); var other = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        EventsPanelCardInfo Card(Guid context, string state = "configured") => new() { Kind = EventsPanelCardKind.TRUNK,
            Label = "DID · 551140001234", Channels = [$"entry:did:{context:N}:11111111111111111111111111111111:{state}"] };
        var cards = new Dictionary<Guid, EventsPanelCardInfo[]> { [own] = [Card(own)], [other] = [Card(other)] };
        var row = new ObservedResource("carrier-call", "pbx", "call", "PJSIP/gateway-whatsapp-official-00000001", "Up", "caller", "551140001234", own.ToString("N"), now, now, new Dictionary<string, string>());
        var observation = AuthorizedObservation.ForPortfolio(cards, [row], new CurrentAuthorization(false, [own, other], []));
        var tiles = OperatorTile.Create(observation.Cards, observation.ByCard, true, observation.CardContexts);
        check(tiles.Length == 2 && tiles.Select(t => t.Id).Distinct().Count() == 2, "Same-number inbound entries retain company identities");
        check(tiles.All(t => t.Kind == EventsPanelCardKind.TRUNK && t.Status.Tone == "unknown" && t.Status.Label.Contains("cadastrada") && !t.HasObservedActivity), "Inbound inventory is a trunk, never fabricated online state or activity");
        check(observation.Rows.Length == 0, "Shared gateway calls never match inventory identities even with the same destination");
        check(OperatorTile.Create([Card(own, "expired")], new Dictionary<EventsPanelCardInfo,List<ObservedResource>>()).Single().Status.Label == "DID vencido", "Expired DID is explicit");
        var whatsapp = new EventsPanelCardInfo { Kind = EventsPanelCardKind.TRUNK, Label = "WhatsApp", Channels = [$"entry:whatsapp:{own:N}:{new string('a',64)}:disabled"] };
        check(OperatorTile.Create([whatsapp], new Dictionary<EventsPanelCardInfo,List<ObservedResource>>()).Single().Status.Label == "Entrada desabilitada", "Disabled WhatsApp is explicit");
        check(InboundCatalogueEntry.State(["PJSIP/gateway-whatsapp-official"]) is null, "Actual trunk channels keep their observed-state behavior");
        observation = AuthorizedObservation.ForPortfolio(cards, [], new CurrentAuthorization(false, [own], []));
        check(observation.Cards.Length == 1, "Foreign inbound catalogue is excluded after permission removal");
    }
}
