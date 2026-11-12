using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Operations;

static class QueueResourceIdentityChecks
{
    public static void Run(Action<bool, string> check)
    {
        var owner = Guid.NewGuid(); var queue = Guid.NewGuid();
        var key = $"queue:{owner:N}:{queue:N}";
        var tile = new OperatorTile("known", "Suporte", "google-voip", EventsPanelCardKind.QUEUE, []) { Keys = [key] };
        var identity = QueueResourceIdentity.From(tile);
        check(identity == new QueueResourceIdentity(owner, queue), "Canonical queue identity survives absence of live observations");
        check(identity!.ManagementUrl("https://blazor.sufficit.com.br/") ==
            $"https://blazor.sufficit.com.br/pages/telephony/queue/edit?contextid={owner:N}&queueid={queue:N}",
            "Management deep link selects exact company and queue on real Blazor edit route");
        var retained = new KnownBoardResource("central", tile.Id, tile.Title, tile.Node, tile.Kind, tile.Keys, DateTimeOffset.UtcNow).Tile();
        check(QueueResourceIdentity.From(retained) == identity, "Persisted native queue retains its management destination without calls or states");
        check(QueueResourceIdentity.From(tile with { Keys = ["0000006500"] }) is null, "Legacy numeric queue never becomes an invented GUID");
        check(QueueResourceIdentity.From(tile with { Keys = [$"queue:{Guid.Empty:N}:{queue:N}"] }) is null, "Missing company does not authorize an edit destination");
        check(QueueResourceIdentity.From(tile with { Keys = [key, $"queue:{owner:N}:{Guid.NewGuid():N}"] }) is null, "Conflicting identities never choose an arbitrary edit destination");
        check(QueueResourceIdentity.From(tile with { Kind = EventsPanelCardKind.PEER }) is null, "Endpoint cards cannot become queue management links");
    }
}
