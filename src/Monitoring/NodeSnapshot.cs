namespace Sufficit.Telephony.Panel.Monitoring;
public sealed record NodeSnapshot(string Id, string Name, bool Fresh, bool? AmiUp, double? AgeSeconds,
    double? Calls, double? Channels, double? ResidentBytes, double? Threads, double? Queued, double? StasisQueued,
    double? HighWater, double? ProcessStart, DateTimeOffset? ObservedAt);
