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
        if (now - row.Updated > TimeSpan.FromSeconds(30))
            return "The last channel event is older than 30 seconds. Wait for a current event or reconnect.";
        if (string.IsNullOrWhiteSpace(row.Node)) return "The channel server is not identified.";
        return null;
    }
}
