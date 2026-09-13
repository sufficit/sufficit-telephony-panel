namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Operator-approved exclusive routing, never a browser-supplied display rule.</summary>
public sealed record SupervisionPrefixRule
{
    public string Id { get; init; } = "";
    public Guid ContextId { get; init; }
    public string Node { get; init; } = "";
    public string CardKey { get; init; } = "";
    public string Prefix { get; init; } = "";
}
