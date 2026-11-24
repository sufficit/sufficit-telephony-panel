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
        : Channels.Length > 0 || FailedDial is not null || RefusingCalls is not null || ConfirmedDnd is not null || PresenceStatus is not null;
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
    /// <summary>Do not disturb confirmed by the PBX device state (Custom:DND), not inferred from refusals.</summary>
    public ObservedResource? ConfirmedDnd => Kind == EventsPanelCardKind.PEER
        ? Rows.Where(r => r.Kind == ConfirmedDoNotDisturb.Kind).OrderByDescending(r => r.Updated).FirstOrDefault() : null;
    /// <summary>Native presence of this device (unified extensions: the same on every device): dnd, away, xa, unavailable, available, chat.</summary>
    public ObservedResource? Presence => Kind == EventsPanelCardKind.PEER
        ? Rows.Where(r => r.Kind == ExtensionPresenceState.Kind).OrderByDescending(r => r.Updated).FirstOrDefault() : null;
    /// <summary>"confirmed", "likely" or null: drives the DND tag and red border.</summary>
    public string? DndLevel => ConfirmedDnd is not null || Presence?.State == "dnd" ? "confirmed" : RefusingCalls is not null ? "likely" : null;
    /// <summary>Status line for a declared presence other than available/chat, or null.</summary>
    public (string Label, string? Message)? PresenceStatus => Presence is { } p ? p.State switch
    {
        "dnd" => ("Do not disturb on", Note(p)),
        "unavailable" => ("Unavailable", Note(p)),
        "xa" => ("Extended away", Note(p)),
        "away" => (p.Details.GetValueOrDefault("subtype") is "busy" ? "Busy" : "Away", Note(p)),
        _ => null
    } : ConfirmedDnd is not null ? ("Do not disturb on", null) : null;
    private static string? Note(ObservedResource row) => row.Details.GetValueOrDefault("message") is { Length: > 0 } note ? note : null;
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
    /// <summary>pjsip, webrtc, sip or iax2 for extension tiles grouped by device; null for trunks, queues and older ids.</summary>
    public string? Device => Id.Split('|').FirstOrDefault(p => p.StartsWith(DevicePrefix, StringComparison.Ordinal))?[DevicePrefix.Length..];
    /// <summary>Voice nodes where this device was observed (the tile node is the most relevant of them).</summary>
    public string[] Nodes { get; init; } = [];
    public bool SeenOn(string node) => Node == node || Nodes.Contains(node);
    /// <summary>Short device and node line for extension tiles: "WebRTC · eveo-voip" (+N when seen on more nodes).</summary>
    public string Where => (Device switch { ExtensionPresenceState.WebRtc => "WebRTC · ", ExtensionPresenceState.Pjsip => "PJSIP · ",
        ExtensionPresenceState.Sip => "SIP · ", "iax2" => "IAX2 · ", _ => "" }) + Node + (Nodes.Length > 1 ? $" +{Nodes.Length - 1}" : "");
    private const string DevicePrefix = "device:";

    /// <summary>Device of an extension row: presence by its key, everything else by its endpoint key or channel.</summary>
    private static string? DeviceOf(ObservedResource row) => row.Kind switch
    {
        ExtensionPresenceState.Kind => ExtensionPresenceState.Device(row.Key),
        ConfirmedDoNotDisturb.Kind => null,
        _ => ExtensionPresenceState.DeviceOfEndpoint(row.Key)
    };

    /// <summary>
    /// Extension tiles: one per device type (PJSIP phone, WebRTC, chan_sip), never one per voice node; in PJSIP every
    /// device is individual, and the same device may appear on several nodes. Presence rows are per device: they join
    /// their device's tile and never create one of their own, unless presence is the only evidence (then a single tile,
    /// WebRTC first). Rows of no device (PBX DND, call results without an endpoint key) join every tile. Latest row per
    /// presence key only (every node holds the same state). Trunks, queues and the customer inbound entry keep node tiles.
    /// </summary>
    private static IEnumerable<(string Node, string? Device, string[] Nodes, ObservedResource[] Rows)> Groups(EventsPanelCardInfo card, List<ObservedResource> rows)
    {
        if (InboundCatalogueEntry.State(card.Channels) is not null)
            return [(rows.Select(r => r.Node).Distinct().Count() == 1 ? rows[0].Node : "", null, [], rows.ToArray())];
        if (card.Kind != EventsPanelCardKind.PEER)
            return rows.GroupBy(r => r.Node).Select(g => (g.Key, (string?)null, new[] { g.Key }, g.ToArray()));
        var presence = rows.Where(r => r.Kind == ExtensionPresenceState.Kind).GroupBy(r => r.Key)
            .Select(g => g.OrderByDescending(r => r.Updated).First()).ToArray();
        var wide = rows.Where(r => r.Kind != ExtensionPresenceState.Kind && DeviceOf(r) is null)
            .GroupBy(r => (r.Kind, r.Key)).Select(g => g.OrderByDescending(r => r.Updated).First()).ToArray();
        var local = rows.Where(r => r.Kind != ExtensionPresenceState.Kind && DeviceOf(r) is not null).ToArray();
        if (local.Length == 0)
        {
            if (presence.Length == 0 && wide.Length == 0) return [];
            var only = presence.OrderBy(r => DeviceOf(r) == ExtensionPresenceState.WebRtc ? 0 : 1).ThenByDescending(r => r.Updated).FirstOrDefault();
            var all = (only is null ? wide : wide.Append(only)).ToArray();
            var at = all.OrderByDescending(r => r.Updated).First().Node;
            return [(at, only is null ? null : DeviceOf(only), new[] { at }, all)];
        }
        return local.GroupBy(r => DeviceOf(r)!).Select(g =>
        {
            // The tile node is where the device is now: an active call first, then its latest registration evidence.
            var node = (g.Where(r => r.Kind == "call").OrderByDescending(r => r.Updated).FirstOrDefault()
                ?? g.OrderByDescending(r => r.Updated).First()).Node;
            var nodes = g.Select(r => r.Node).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            return (node, (string?)g.Key, nodes, g.Concat(presence.Where(r => DeviceOf(r) == g.Key)).Concat(wide).ToArray());
        });
    }

    public static OperatorTile[] Create(IEnumerable<EventsPanelCardInfo> cards,
        IReadOnlyDictionary<EventsPanelCardInfo, List<ObservedResource>> byCard, bool isolateContexts = false,
        IReadOnlyDictionary<EventsPanelCardInfo, Guid>? cardContexts = null) => cards
        .Where(c => c.Kind is EventsPanelCardKind.PEER or EventsPanelCardKind.TRUNK or EventsPanelCardKind.QUEUE)
        .SelectMany(c => (byCard.TryGetValue(c, out var rows) && rows.Count > 0
            ? Groups(c, rows)
            : [(Node: InboundCatalogueEntry.State(c.Channels) is not null ? "Entrada do cliente" : "Sem nó observado", Device: (string?)null, Nodes: Array.Empty<string>(), Rows: Array.Empty<ObservedResource>())])
            .Select(g => new OperatorTile(c.Kind + "|" + string.Join(";", c.Channels) + "|" + (g.Device is { } device ? DevicePrefix + device : g.Node)
                + (isolateContexts ? "|" + (cardContexts is not null && cardContexts.TryGetValue(c, out var context) ? context.ToString("N") : g.Rows.FirstOrDefault()?.Account) : ""),
                c.Label, g.Node, c.Kind, g.Rows) { Keys = c.Channels.ToArray(), Nodes = g.Nodes }))
        .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Device, StringComparer.Ordinal).ThenBy(t => t.Node, StringComparer.Ordinal).ToArray();
}
