using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Identity only: never persist channels, call parties, peer states or actions.</summary>
public sealed record KnownBoardResource(string Scope, string Id, string Title, string Node,
    EventsPanelCardKind Kind, string[] Keys, DateTimeOffset LastSeen)
{
    public OperatorTile Tile() => new(Id, Title, Node, Kind, []) { Keys = Keys.ToArray(), LastSeen = LastSeen };
}
