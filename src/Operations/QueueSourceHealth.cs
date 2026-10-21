namespace Sufficit.Telephony.Panel.Operations;

public static class QueueSourceHealth
{
    public static string? Problem(QueueBoardSample sample, DateTimeOffset now)
    {
        if (!sample.Available || sample.Snapshot is null) return "Source query failed or returned an invalid response.";
        var snapshot = sample.Snapshot;
        if (!snapshot.ControllerConnected) return "Queue controller is disconnected.";
        if (snapshot.ObserverOnly) return "Registration observation only; no live queue activity.";
        if (snapshot.ObservedAt == default) return "Source did not report a valid observation time.";
        if (snapshot.ObservedAt > now.AddSeconds(5)) return "Source observation time is in the future. Check server clocks.";
        if (snapshot.ObservedAt < now.AddSeconds(-20)) return "Source observation is older than 20 seconds.";
        return null;
    }
}
