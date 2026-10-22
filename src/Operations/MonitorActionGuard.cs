using System.Text.RegularExpressions;

namespace Sufficit.Telephony.Panel.Operations;

public static class MonitorActionGuard
{
    public static bool Allows(ObservedResource row, Guid context, bool connected, DateTimeOffset now) =>
        BlockReason(row, context, connected, now) is null;

    public static string? BlockReason(ObservedResource row, Guid context, bool connected, DateTimeOffset now)
    {
        if (!connected) return "Reconnect to the event stream before supervising.";
        if (row.Kind != "call") return "Select a call channel to supervise.";
        if (!Regex.IsMatch(row.Key, @"^(PJSIP|SIP)/[a-zA-Z0-9_.-]+-[0-9a-fA-F]{8}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10)))
            return "Only SIP/PJSIP channels support this supervision action. Local channels are not supported.";
        if (context == Guid.Empty || !Guid.TryParse(row.Account, out var account) || account == Guid.Empty)
            return "The channel has no verified company association. Supervision is blocked.";
        if (account != context) return "The channel belongs to another company.";
        if (now < row.Updated) return "The event time is in the future. Check server clocks.";
        // No age limit: an answered call emits no further events, so a "recent event" rule blocked every
        // conversation after 30 s. Liveness comes from the projection itself: HangupEvent removes the
        // channel, rows idle for 2 h expire, and a disconnected stream blocks above.
        if (string.IsNullOrWhiteSpace(row.Node)) return "The channel server is not identified.";
        return null;
    }
}
