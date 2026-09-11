namespace Sufficit.Telephony.Panel.Monitoring;
public sealed record HistorySeries(DateTimeOffset Start, DateTimeOffset End, IReadOnlyList<HistoryPoint> Points);
