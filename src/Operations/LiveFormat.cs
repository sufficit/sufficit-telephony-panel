using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Panel.Localization;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Localized, state-free formatting for observed telephony rows; shared by the live views.</summary>
public sealed class LiveFormat(PanelText Text)
{
    public static string PhaseLabel(string phase) => phase switch
    {
        "greeting" => "Queue greeting", "announcing" => "Queue announcement", "offering" => "Calling an agent",
        "answered" => "Answered in queue", "waiting" => "Waiting in queue", _ => "Observed call flow"
    };
    public static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    public string Timestamp(DateTimeOffset? value) => value?.ToLocalTime().ToString("G", Text.Culture) ?? Text["Not reported"];
    public string KindLabel(EventsPanelCardKind kind) => kind switch { EventsPanelCardKind.TRUNK => Text["Trunk"], EventsPanelCardKind.QUEUE => Text["Call queue"], _ => Text["Extension"] };
    public static string Duration(DateTimeOffset from) => (DateTimeOffset.UtcNow - from).ToString(@"hh\:mm\:ss");
    public string DetailValue(string value) => value switch { "true" => Text["yes"], "false" => Text["no"], _ => value };
    public string ResourceState(ObservedResource row) => row.Kind == "agent" ? row.State.ToLowerInvariant() switch
    {
        "1" or "notinuse" or "not_inuse" => Text["Idle in event"], "2" or "inuse" => Text["In use"], "3" or "busy" => Text["Busy"],
        "4" or "invalid" => Text["Invalid"], "5" or "unavailable" => Text["Unavailable"], "6" or "ringing" => Text["Ringing"], "7" or "ringinuse" => Text["Ringing while in use"], "8" or "onhold" => Text["On hold"], "0" => Text["Unknown state"], _ => State(row.State)
    } : Text.Observed(row.Peer?.ReachabilityLabel ?? State(row.State));
    public string State(string value) => value.ToLowerInvariant() switch { "up" or "6" => Text["Channel answered"], "ringing" or "5" => Text["Ringing"], "ring" or "4" => Text["Calling"], "down" or "0" => Text["Inactive in event"], "reachable" or "registered" => Text["Available in event"], "unreachable" or "unregistered" => Text["Unavailable in event"], "unknown" or "" => Text["Not reported"], _ => value };
    public string DetailLabel(string key) => key switch { "paused" => Text["paused"], "calls" => Text["Calls"], "completed" => Text["answered"], "abandoned" => Text["abandoned"], "holdtime" => Text["wait (s)"], "talktime" => Text["talk (s)"], "penalty" => Text["priority"], "strategy" => Text["strategy"], "membername" => Text["agent"], "interface" => "interface", "status" => "status", _ => key };
}
