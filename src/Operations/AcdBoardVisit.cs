namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdBoardVisit(string Id, string QueueId, string Phase, DateTimeOffset EnteredAt,
    string? ChannelId = null, string? ChannelName = null, string? Caller = null);
