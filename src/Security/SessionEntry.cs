using Microsoft.AspNetCore.Authentication;
namespace Sufficit.Telephony.Panel.Security;
internal sealed class SessionEntry(AuthenticationTicket ticket)
{
    public AuthenticationTicket Ticket { get; } = ticket;
    public DateTimeOffset CheckedAt { get; set; }
    public bool Manager { get; set; }
}
