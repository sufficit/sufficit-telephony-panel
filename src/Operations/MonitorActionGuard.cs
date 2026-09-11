using System.Text.RegularExpressions;

namespace Sufficit.Telephony.Panel.Operations;

public static class MonitorActionGuard
{
    public static bool Allows(ObservedResource row, Guid context, bool connected, DateTimeOffset now) =>
        connected && context != Guid.Empty && row.Kind == "call"
        && Guid.TryParse(row.Account, out var account) && account == context
        && now >= row.Updated && now - row.Updated <= TimeSpan.FromSeconds(30)
        && !string.IsNullOrWhiteSpace(row.Node)
        && Regex.IsMatch(row.Key, @"^(PJSIP|SIP)/[a-zA-Z0-9_.-]+-[0-9a-fA-F]{8}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10));
}
