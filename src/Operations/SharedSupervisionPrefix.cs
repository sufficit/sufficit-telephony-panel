using System.Text.RegularExpressions;
using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Managers only: continuous supervision of a shared trunk by the prefix of its observed channels
/// (e.g. "SIP/IN-DATORA-"). Shared trunks carry several companies' calls, so this is never offered to
/// customers; the server re-checks the manager role and audits every request with the companies seen.
/// </summary>
public static partial class SharedSupervisionPrefix
{
    [GeneratedRegex(@"^((?:PJSIP|SIP|IAX2)/[a-zA-Z0-9_.-]+-)[0-9a-fA-F]{8}$|^(IAX2/[a-zA-Z0-9_.:-]+-)\d{1,5}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelName();

    /// <summary>Prefix of a concrete channel name, or null when it is not a trunk-style SIP/PJSIP/IAX2 channel.</summary>
    public static string? PrefixOf(string channel)
    {
        var match = ChannelName().Match(channel);
        if (!match.Success) return null;
        var prefix = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return SupervisionPrefixPolicy.SafePrefix(prefix) ? prefix : null;
    }

    /// <summary>Prefixes currently observed on a trunk card, on the card's own server.</summary>
    public static string[] For(OperatorTile tile, bool manager, bool connected) =>
        !manager || !connected || tile.Kind != EventsPanelCardKind.TRUNK ? [] : tile.Rows
            .Where(row => row.Kind == "call" && row.Node == tile.Node)
            .Select(row => PrefixOf(row.Key)).OfType<string>()
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(8).ToArray();

    /// <summary>Companies with calls on the prefix right now (audit only; unknown legs reported as such).</summary>
    public static string[] Companies(IEnumerable<ObservedResource> rows, string node, string prefix) => rows
        .Where(row => row.Kind == "call" && row.Node == node && row.Key.StartsWith(prefix, StringComparison.Ordinal))
        .Select(row => Guid.TryParse(row.Account, out var id) && id != Guid.Empty ? id.ToString("N") : "unknown")
        .Distinct().Order().ToArray();
}
