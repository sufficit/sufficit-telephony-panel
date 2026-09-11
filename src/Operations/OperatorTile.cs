using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record OperatorTile(string Id, string Title, string Node, EventsPanelCardKind Kind,
    ObservedResource[] Rows)
{
    public string[] Keys { get; init; } = [];
    public DateTimeOffset? LastSeen { get; init; }
    public bool Remembered => Rows.Length == 0 && LastSeen.HasValue;
    public ObservedResource[] Channels => Rows.Where(r => r.Kind == "call").ToArray();
    public PeerEvidence? Peer => Rows.Where(r => r.Peer is not null).OrderByDescending(r => r.Updated).FirstOrDefault()?.Peer;
    public DateTimeOffset? Updated => Rows.Length == 0 ? LastSeen : Rows.Max(r => r.Updated);
    public (string Tone, string Label) Status => Remembered ? ("unknown", "Já visto · sem estado atual")
        : Kind == EventsPanelCardKind.QUEUE ? ("unknown", "Fila observada · não comprova atividade") : Describe(Rows);
    public static (string Tone, string Label) Describe(ObservedResource[] rows)
    {
        // This is observed evidence, never a statement that the whole extension is free.
        var calls = rows.Where(r => r.Kind == "call").ToArray();
        if (calls.Any(r => r.State.ToLowerInvariant() is "ring" or "ringing" or "4" or "5")) return ("ringing", "Tocando no evento");
        if (calls.Length > 0) return ("busy", $"{calls.Length} canal(is) observado(s)");
        var peer = rows.Where(r => r.Peer is not null).OrderByDescending(r => r.Updated).FirstOrDefault()?.Peer;
        if (peer?.DeviceLabel is "Tocando" or "Tocando em uso") return ("ringing", peer.DeviceLabel);
        if (peer?.DeviceLabel is "Em uso" or "Ocupado" or "Em espera") return ("busy", peer.DeviceLabel);
        if (peer?.Reachability == "offline") return ("offline", "Sem resposta no teste");
        if (peer?.Registration == "unregistered") return ("offline", "Não registrado no evento");
        if (peer?.Reachability == "online") return ("online", "Alcançável no teste");
        if (peer?.Registration == "registered") return ("online", "Registrado no evento");
        return ("unknown", "Sem informação de alcance");
    }
    public static OperatorTile[] Create(IEnumerable<EventsPanelCardInfo> cards,
        IReadOnlyDictionary<EventsPanelCardInfo, List<ObservedResource>> byCard) => cards
        .Where(c => c.Kind is EventsPanelCardKind.PEER or EventsPanelCardKind.TRUNK or EventsPanelCardKind.QUEUE)
        .SelectMany(c => (byCard.TryGetValue(c, out var rows) && rows.Count > 0
            ? rows.GroupBy(r => r.Node).Select(g => (Node: g.Key, Rows: g.ToArray()))
            : [(Node: "Sem nó observado", Rows: Array.Empty<ObservedResource>())])
            .Select(g => new OperatorTile(c.Kind + "|" + string.Join(";", c.Channels) + "|" + g.Node,
                c.Label, g.Node, c.Kind, g.Rows) { Keys = c.Channels.ToArray() }))
        .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Node, StringComparer.Ordinal).ToArray();
}
