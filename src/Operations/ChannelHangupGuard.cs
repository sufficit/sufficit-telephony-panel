using System.Text.RegularExpressions;
namespace Sufficit.Telephony.Panel.Operations;

public static class ChannelHangupGuard
{
    public static string? BlockReason(ObservedResource row, bool manager, bool connected)
    {
        if (!manager) return "Only managers can end a channel.";
        if (!connected) return "Reconnect before ending a channel.";
        if (row.Kind != "call" || !Safe(row.Node, @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$")
            || !Safe(row.Key, @"^[A-Za-z][A-Za-z0-9_]{0,20}/[A-Za-z0-9_@+./:;-]{1,210}$")
            || !Safe(row.Details.GetValueOrDefault("uniqueid"), @"^[A-Za-z0-9_.:-]{1,128}$"))
            return "This channel has no verified identity for termination.";
        return null;
    }
    private static bool Safe(string? value,string pattern) => value is not null && !value.Contains('\r') && !value.Contains('\n')
        && Regex.IsMatch(value,pattern,RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
}
