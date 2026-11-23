using Sufficit.Telephony.Panel;

namespace Sufficit.Telephony.Panel.Operations;

public sealed record OperatorTile(string Id, string Title, string Node, EventsPanelCardKind Kind,
    ObservedResource[] Rows)
{
    public string[] Keys { get; init; } = [];
    public BoardCallFlow[] Flows { get; init; } = [];
    public ObservedResource? QueueSummary { get; init; }
    public bool QueueStateIncomplete => Rows.Any(r => r.Kind == "queue" && r.Details.GetValueOrDefault("source") == "queue" && r.State != "current");
    public ObservedResource? Queue => QueueSummary ?? Rows.FirstOrDefault(r => r.Kind == "queue" && r.Details.GetValueOrDefault("source") == "queue");
    public bool HasObservedActivity => Kind == EventsPanelCardKind.QUEUE
        ? Queue is { } queue ? queue.State == "current" && int.TryParse(queue.Details.GetValueOrDefault("pending"), out var pending) && pending > 0
            : QueueCalls.Length > 0
        : Channels.Length > 0 || FailedDial is not null || RefusingCalls is not null;
    public ObservedResource[] QueueCalls => Rows.Where(r => r.Kind == "waiting" && r.State is not ("answered" or "connected")).ToArray();
    public ObservedResource[] DetailChannels => Flows.SelectMany(f => f.Channels)
        .Concat(Kind == EventsPanelCardKind.QUEUE ? [] : Channels).DistinctBy(r => (r.Node, r.Id)).ToArray();
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
    /// <summary>A brief dialing outcome on this endpoint, never an active call or another extension's outcome.</summary>
    public ObservedResource? FailedDial => Kind == EventsPanelCardKind.PEER && Channels.Length == 0
        ? Rows.Where(r => r.Kind == "call-result" && DateTimeOffset.UtcNow - r.Updated < RecentDialFailure.Lifetime
            && DateTimeOffset.UtcNow >= r.Updated && RecentDialFailure.Label(r.State) is not null)
            .OrderByDescending(r => r.Updated).FirstOrDefault() : null;
    /// <summary>Busy answers while this endpoint had no other call ("Do not disturb" on the phone), never a device read.</summary>
    public ObservedResource? RefusingCalls => Kind == EventsPanelCardKind.PEER && Channels.Length == 0
        ? Rows.Where(r => r.Kind == RecentCallRefusal.Kind && DateTimeOffset.UtcNow - r.Updated < RecentCallRefusal.Lifetime
            && DateTimeOffset.UtcNow >= r.Updated.AddSeconds(-2)).OrderByDescending(r => r.Updated).FirstOrDefault() : null;
    public ObservedResource[] Channels => Rows.Where(r => r.Kind == "call").ToArray();
    public PeerEvidence? Peer => Rows.Where(r => r.Peer is not null).OrderByDescending(r => r.Updated).FirstOrDefault()?.Peer;
    public DateTimeOffset? Updated => Rows.Length == 0 ? LastSeen : Rows.Max(r => r.Updated);
    public (string Tone, string Label) Status => Channels.Length == 0 && InboundCatalogueEntry.State(Keys) is { } entry
        ? ("unknown", Rows.Any(r => r.Kind == "inbound" && r.State == "current") ? "No incoming calls" : InboundCatalogueEntry.Label(entry))
        : Remembered ? ("unknown", "Já visto · sem estado atual")
        : Queue is { } queue ? queue.State != "current" ? ("unknown", "QUEUE state unavailable")
            : queue.Details.GetValueOrDefault("pending", "0") != "0"
                ? ("busy", "QUEUE queue activity") : ("online", "No active QUEUE visits")
        : Kind == EventsPanelCardKind.QUEUE ? (QueueCalls.Length > 0 ? ("busy", "Queue activity observed") : ("unknown", "Fila observada · não comprova atividade"))
        : CallStatus;
    private static bool IsDialing(ObservedResource channel) => channel.State.ToLowerInvariant()
        is "dialing" or "3" or "ring" or "4" or "dialing offhook" or "dialingoffhook" or "8" or "pre-ring" or "prering" or "9";
    private static bool IsRinging(ObservedResource channel) => channel.State.ToLowerInvariant() is "ringing" or "5";
    private (string Tone, string Label) CallStatus
    {
        get
        {
            var observed = Describe(Rows);
            if (Kind != EventsPanelCardKind.PEER || observed.Tone != "dialing") return observed;
            // The caller can remain Ring after the destination enters Ringing. Use only
            // authorized, same-node linked legs; preserve raw states and distinguish ringback.
            foreach (var caller in Channels.Where(IsDialing))
            {
                var linked = caller.Details.GetValueOrDefault("linkedid");
                if (!LiveProjection.Meaningful(linked)) continue;
                if (Flows.Where(f => f.Correlated && f.Channels.Any(c => c.Id == caller.Id))
                    .SelectMany(f => f.Channels).Any(destination => destination.Id != caller.Id
                        && destination.Node == caller.Node
                        && destination.Details.GetValueOrDefault("linkedid") == linked
                        && BoardRuleEngine.IsEndpointKey(destination.Key) && IsRinging(destination)))
                    return ("ringing", "Destination ringing");
            }
            return observed;
        }
    }
    public static (string Tone, string Label) Describe(ObservedResource[] rows)
    {
        // This is observed evidence, never a statement that the whole extension is free.
        var calls = rows.Where(r => r.Kind == "call").ToArray();
        if (calls.Any(IsRinging)) return ("ringing", "Tocando no evento");
        if (calls.Any(IsDialing))
            return ("dialing", "Dialing");
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
        IReadOnlyDictionary<EventsPanelCardInfo, List<ObservedResource>> byCard, bool isolateContexts = false,
        IReadOnlyDictionary<EventsPanelCardInfo, Guid>? cardContexts = null) => cards
        .Where(c => c.Kind is EventsPanelCardKind.PEER or EventsPanelCardKind.TRUNK or EventsPanelCardKind.QUEUE)
        .SelectMany(c => (byCard.TryGetValue(c, out var rows) && rows.Count > 0
            ? rows.GroupBy(r => InboundCatalogueEntry.State(c.Channels) is not null ? "" : r.Node)
                .Select(g => (Node: g.Select(r => r.Node).Distinct().Count() == 1 ? g.First().Node : "", Rows: g.ToArray()))
            : [(Node: InboundCatalogueEntry.State(c.Channels) is not null ? "Entrada do cliente" : "Sem nó observado", Rows: Array.Empty<ObservedResource>())])
            .Select(g => new OperatorTile(c.Kind + "|" + string.Join(";", c.Channels) + "|" + g.Node
                + (isolateContexts ? "|" + (cardContexts is not null && cardContexts.TryGetValue(c, out var context) ? context.ToString("N") : g.Rows.FirstOrDefault()?.Account) : ""),
                c.Label, g.Node, c.Kind, g.Rows) { Keys = c.Channels.ToArray() }))
        .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Node, StringComparer.Ordinal).ToArray();
}
