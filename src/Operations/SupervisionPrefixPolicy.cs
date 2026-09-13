using System.Text.RegularExpressions;
using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class SupervisionPrefixPolicy
{
    private readonly SupervisionPrefixRule[] rules;
    public SupervisionPrefixPolicy(IEnumerable<SupervisionPrefixRule> source)
    {
        rules = source.ToArray();
        if (rules.Length > 256 || rules.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != rules.Length
            || rules.Any(r => !Regex.IsMatch(r.Id, @"^[a-zA-Z0-9_-]{1,64}$") || r.ContextId == Guid.Empty
                || string.IsNullOrWhiteSpace(r.Node) || r.Node.Length > 100 || string.IsNullOrWhiteSpace(r.CardKey)
                || r.CardKey.Length > 256 || !SafePrefix(r.Prefix)))
            throw new InvalidOperationException("Invalid protected supervision prefix configuration.");
    }
    public static bool SafePrefix(string prefix) => prefix.Length <= 160
        && Regex.IsMatch(prefix, @"^(?:PJSIP|SIP|IAX2)/[a-zA-Z0-9_.-]+-$|^Local/[a-zA-Z0-9_.+-]+@[a-zA-Z0-9_.-]+-$",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10));
    public SupervisionPrefixRule Get(string id) => rules.SingleOrDefault(r => r.Id == id)
        ?? throw new InvalidOperationException("Continuous supervision is not authorized for this resource.");
    public SupervisionPrefixRule[] For(OperatorTile tile, Guid? context) => rules.Where(r => r.Node == tile.Node
        && (!context.HasValue || r.ContextId == context) && tile.Keys.Contains(r.CardKey, StringComparer.Ordinal)).ToArray();
    public static bool Authorized(SupervisionPrefixRule rule, IEnumerable<EventsPanelCardInfo> cards) =>
        cards.Any(card => card.Channels.Contains(rule.CardKey));
    public static bool Observed(SupervisionPrefixRule rule, IEnumerable<ObservedResource> rows, bool connected, DateTimeOffset now) =>
        connected && rows.Any(row => row.Node == rule.Node && now >= row.Updated && now - row.Updated <= TimeSpan.FromSeconds(30)
            && (row.Key == rule.CardKey || row.Key.StartsWith(rule.Prefix, StringComparison.Ordinal))
            && (!Guid.TryParse(row.Account, out var account) || account == rule.ContextId))
        && !rows.Any(row => row.Node == rule.Node && row.Key.StartsWith(rule.Prefix, StringComparison.Ordinal)
            && Guid.TryParse(row.Account, out var account) && account != rule.ContextId);
}
