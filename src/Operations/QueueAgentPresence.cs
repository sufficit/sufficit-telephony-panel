namespace Sufficit.Telephony.Panel.Operations;

public sealed record QueueContact(string Endpoint, string? IpAddress, string? ViaAddress,
    string? UserAgent, DateTimeOffset? ExpiresAt);
public sealed record QueueRegistration(DateTimeOffset ObservedAt, string Event, bool? Registered,
    bool? Reachable, QueueContact[]? Contacts);
public sealed record QueueAgentPresence(string ContextId, string Extension, DateTimeOffset ObservedAt,
    bool? Registered, bool? Reachable, bool? Busy, QueueContact[]? Contacts = null,
    QueueRegistration[]? RegistrationHistory = null, DateTimeOffset? HistorySince = null)
{
    public bool Fresh(DateTimeOffset now) => ObservedAt >= now.AddSeconds(-15) && ObservedAt <= now.AddSeconds(2);
    public string State(DateTimeOffset now, bool sourceFresh) => !sourceFresh || !Fresh(now) ? "No current data"
        : Busy == true ? "Busy" : Registered == false ? "Unregistered" : Reachable == false ? "Unreachable"
        : Registered == true && Reachable == true && Busy == false ? "Available"
        : Registered == true ? "Registered" : "Unknown";
}
