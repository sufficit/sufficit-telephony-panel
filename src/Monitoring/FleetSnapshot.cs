namespace Sufficit.Telephony.Panel.Monitoring;
public sealed record FleetSnapshot(DateTimeOffset FetchedAt, IReadOnlyList<NodeSnapshot> Nodes, IReadOnlyList<PanelAlert> Alerts, string? Error);
