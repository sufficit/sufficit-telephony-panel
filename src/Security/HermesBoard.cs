using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Sufficit.Telephony.Panel.Security;

internal static class HermesBoard
{
    public const string FramePolicy = "frame-ancestors https://chatwoot.sufficit.com.br https://chat.sufficit.com.br https://hermes.sufficit.com.br https://eveo-chatwoot.sufficit.com.br";

    public static Guid? Context(string uri) => Context(QueryHelpers.ParseQuery(new Uri(uri).Query));

    public static Guid? Context(IEnumerable<KeyValuePair<string, StringValues>> query)
    {
        var parameters = query.ToDictionary(StringComparer.OrdinalIgnoreCase);
        return parameters.TryGetValue("embed", out var embed) && embed.Count == 1 && embed[0] == "hermes"
            && parameters.TryGetValue("contextid", out var context) && context.Count == 1
            && Guid.TryParse(context[0], out var id) && id != Guid.Empty ? id : null;
    }
}
