using Microsoft.AspNetCore.WebUtilities;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Synthetic identity lookup, called only by the Development-authorized preview.</summary>
public static class QueueIdentityFixture
{
    public static QueueResourceIdentity? Read(string uri, string extension)
    {
        var scenario = QueryHelpers.ParseQuery(new Uri(uri).Query).GetValueOrDefault("queueidentitypreview").ToString();
        if (scenario == "error") throw new HttpRequestException("Synthetic catalogue failure.");
        if (scenario == "ambiguous") throw new HttpRequestException("Synthetic ambiguous catalogue.", null, System.Net.HttpStatusCode.Conflict);
        return scenario is "resolved" or "legacy" ? new(Guid.Parse("11111111-1111-7111-8111-111111111111"),
            Guid.Parse("99999999-9999-7999-8999-999999996500"), scenario == "resolved") { Extension = extension } : null;
    }
}
