namespace Sufficit.Telephony.Panel.Operations;

public sealed record BoardRule
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString("N");
    public string Label { get; init; } = "";
    public string Kind { get; init; } = "trunk";
    public string Node { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public string[] Patterns { get; init; } = [];
}
