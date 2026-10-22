using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record OperatorTile(string Id, string Title, string Node, EventsPanelCardKind Kind,
    ObservedResource[] Rows)
{
    public string[] Keys { get; init; } = [];
    public BoardCallFlow[] Flows { get; init; } = [];
    public ObservedResource? Queue => Rows.FirstOrDefault(r => r.Kind == "queue" && r.Details.GetValueOrDefault("source") == "queue");
    public bool HasObservedActivity => Channels.Length > 0 || QueueCalls.Length > 0 ||
        Queue is { State: "current" } queue && (queue.Details.GetValueOrDefault("pending", "0") != "0" || queue.Details.GetValueOrDefault("connected", "0") != "0");
    public ObservedResource[] QueueCalls => Rows.Where(r => r.Kind == "waiting").ToArray();
    public ObservedResource[] DetailChannels => Flows.SelectMany(f => f.Channels).Concat(Channels).DistinctBy(r => r.Id).ToArray();
    public DateTimeOffset? LastSeen { get; init; }
    private ObservedResource[] PeerRows => Rows.Where(r => r.Kind == "peer").OrderByDescending(r => r.Updated).ToArray();
    /// <summary>Last contact address reported by the PBX: chan_sip "ip:port" or the PJSIP contact URI host.</summary>
    public string? ContactAddress => PeerRows.Select(r => r.Details.GetValueOrDefault("address") is { Length: > 0 } address ? address
        : r.Details.GetValueOrDefault("uri") is { Length: > 0 } uri ? ContactHost(uri) : null).FirstOrDefault(a => a is not null);
    public string? UserAgent => PeerRows.Select(r => r.Details.GetValueOrDefault("useragent")).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a));
    /// <summary>"sip:6001@177.1.2.3:5060;transport=udp" → "177.1.2.3:5060".</summary>
    public static string ContactHost(string uri)
    {
        var value = uri.Split(';', 2)[0];
        var at = value.LastIndexOf('@');
        if (at >= 0) value = value[(at + 1)..];
        else if (value.IndexOf(':') is var scheme and > 0 && value[..scheme] is "sip" or "sips") value = value[(scheme + 1)..];
        return value.TrimEnd('>');
    }
    public bool Remembered => Rows.Length == 0 && LastSeen.HasValue;
    public ObservedResource[] Channels => Rows.Where(r => r.Kind == "call").ToArray();
    public PeerEvidence? Peer => Rows.Where(r => r.Peer is not null).OrderByDescending(r => r.Updated).FirstOrDefault()?.Peer;
    public DateTimeOffset? Updated => Rows.Length == 0 ? LastSeen : Rows.Max(r => r.Updated);
    public (string Tone, string Label) Status => Remembered ? ("unknown", "Já visto · sem estado atual")
        : Queue is { } queue ? queue.State != "current" ? ("unknown", "QUEUE state unavailable")
            : queue.Details.GetValueOrDefault("pending") != "0" || queue.Details.GetValueOrDefault("connected") != "0"
                ? ("busy", "QUEUE queue activity") : ("online", "No active QUEUE visits")
        : Kind == EventsPanelCardKind.QUEUE ? (QueueCalls.Length > 0 ? ("busy", "Queue activity observed") : ("unknown", "Fila observada · não comprova atividade")) : Describe(Rows);
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
        IReadOnlyDictionary<EventsPanelCardInfo, List<ObservedResource>> byCard, bool isolateContexts = false) => cards
        .Where(c => c.Kind is EventsPanelCardKind.PEER or EventsPanelCardKind.TRUNK or EventsPanelCardKind.QUEUE)
        .SelectMany(c => (byCard.TryGetValue(c, out var rows) && rows.Count > 0
            ? rows.GroupBy(r => r.Node).Select(g => (Node: g.Key, Rows: g.ToArray()))
            : [(Node: "Sem nó observado", Rows: Array.Empty<ObservedResource>())])
            .Select(g => new OperatorTile(c.Kind + "|" + string.Join(";", c.Channels) + "|" + g.Node
                + (isolateContexts ? "|" + g.Rows.FirstOrDefault()?.Account : ""),
                c.Label, g.Node, c.Kind, g.Rows) { Keys = c.Channels.ToArray() }))
        .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Node, StringComparer.Ordinal).ToArray();
}
