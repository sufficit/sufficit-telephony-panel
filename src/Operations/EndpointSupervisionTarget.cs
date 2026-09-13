using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>A concrete endpoint candidate, never an authorization supplied by the UI.</summary>
public sealed record EndpointSupervisionTarget(Guid ContextId, string Node, string PeerKey)
{
    public string Id => $"endpoint:{ContextId:N}:{Node}:{PeerKey}";
    public string Prefix => PeerKey + "-";
    public bool Authorized(IEnumerable<EventsPanelCardInfo> cards) =>
        ContextId != Guid.Empty && (PeerKey.StartsWith("SIP/", StringComparison.Ordinal) || PeerKey.StartsWith("PJSIP/", StringComparison.Ordinal))
        && SupervisionPrefixPolicy.SafePrefix(Prefix)
        && cards.Any(card => card.Kind == EventsPanelCardKind.PEER && card.Channels.Any(key => MatchesPeerKey(key, PeerKey)));

    // The portal emits ^SIP/<endpoint> as its anchored display pattern. Remove only
    // that anchor and compare literal identity; never evaluate a regex as authority.
    public static bool MatchesPeerKey(string key, string peer) =>
        string.Equals(key.StartsWith('^') ? key[1..] : key, peer, StringComparison.Ordinal);

    public static EndpointSupervisionTarget[] For(OperatorTile tile, Guid? context, bool connected, DateTimeOffset now)
    {
        if (tile.Kind != EventsPanelCardKind.PEER || !connected) return [];
        var accounts = tile.Rows.Select(row => Guid.TryParse(row.Account, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).Distinct().ToArray();
        var scope = context ?? (accounts is [var account] ? account : Guid.Empty);
        if (scope == Guid.Empty) return [];
        return tile.Rows.Where(row => row.Node == tile.Node &&
                (row.Kind == "peer" || MonitorActionGuard.Allows(row, scope, connected, now)))
            .Select(row => new EndpointSupervisionTarget(scope, row.Node,
                row.Kind == "peer" ? row.Key : row.Key[..row.Key.LastIndexOf('-')]))
            .Where(target => target.ValidPeer && target.Observed(tile.Rows, connected, now))
            .Where(target => tile.Keys.Any(key => MatchesPeerKey(key, target.PeerKey)))
            .Distinct().ToArray();
    }

    private bool ValidPeer => (PeerKey.StartsWith("SIP/", StringComparison.Ordinal) || PeerKey.StartsWith("PJSIP/", StringComparison.Ordinal))
        && SupervisionPrefixPolicy.SafePrefix(Prefix);

    public bool Observed(IEnumerable<ObservedResource> source, bool connected, DateTimeOffset now)
    {
        if (!connected || !ValidPeer || ContextId == Guid.Empty) return false;
        var rows = source.Where(row => row.Node == Node && (row.Key == PeerKey || row.Key.StartsWith(Prefix, StringComparison.Ordinal))).ToArray();
        if (rows.Any(row => Guid.TryParse(row.Account, out var account) && account != ContextId)) return false;
        // Peer Updated records the last state transition, not a periodic heartbeat.
        // The connected live projection supplies endpoint identity even when idle.
        // Ownership is separately re-read from scoped portal PEER cards at send time.
        return rows.Any(row => row.Kind == "peer" && row.Key == PeerKey && row.Updated <= now
                && (string.IsNullOrWhiteSpace(row.Account) || Guid.TryParse(row.Account, out var account) && account == ContextId))
            || rows.Any(row => MonitorActionGuard.Allows(row, ContextId, connected, now));
    }
}
