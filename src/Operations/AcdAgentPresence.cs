namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdContact(string Endpoint, string? IpAddress, string? ViaAddress,
    string? UserAgent, DateTimeOffset? ExpiresAt);
public sealed record AcdRegistration(DateTimeOffset ObservedAt, string Event, bool? Registered,
    bool? Reachable, AcdContact[]? Contacts);
public sealed record AcdAgentPresence(string ContextId, string Extension, DateTimeOffset ObservedAt,
    bool? Registered, bool? Reachable, bool? Busy, AcdContact[]? Contacts = null,
    AcdRegistration[]? RegistrationHistory = null, DateTimeOffset? HistorySince = null)
{
    public bool Fresh(DateTimeOffset now) => ObservedAt >= now.AddSeconds(-15) && ObservedAt <= now.AddSeconds(2);
    public string State(DateTimeOffset now, bool sourceFresh) => !sourceFresh || !Fresh(now) ? "No current data"
        : Busy == true ? "Busy" : Registered == false ? "Unregistered" : Reachable == false ? "Unreachable"
        : Registered == true && Reachable == true && Busy == false ? "Available"
        : Registered == true ? "Registered" : "Unknown";
}
