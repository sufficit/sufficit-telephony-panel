namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Explicit queue identity, including retained canonical keys. Never infer GUIDs from legacy numbers.</summary>
public sealed record QueueResourceIdentity(Guid ContextId, Guid QueueId, bool Native = true)
{
    public string? Extension { get; init; }
    public string ManagementUrl(string root) => root.TrimEnd('/') + (Native
        ? $"/pages/telephony/queue/edit?contextid={ContextId:N}&queueid={QueueId:N}"
        : $"/pages/telephony/queue?contextid={ContextId:N}");

    public static string? LegacyExtension(OperatorTile tile)
    {
        if (tile.Kind != EventsPanelCardKind.QUEUE) return null;
        var numbers = tile.Keys.Select(key => key.TrimStart('^').TrimEnd('$'))
            .Select(key => key.StartsWith("Local/", StringComparison.Ordinal) ? key[6..] : key)
            .Where(value => value is { Length: > 0 and <= 20 } && value.All(char.IsAsciiDigit))
            .Distinct(StringComparer.Ordinal).ToArray();
        return numbers.Length == 1 ? numbers[0] : null;
    }

    public static QueueResourceIdentity? From(OperatorTile tile)
    {
        if (tile.Kind != EventsPanelCardKind.QUEUE) return null;
        var found = new HashSet<QueueResourceIdentity>();
        foreach (var key in tile.Keys)
            if (key.Split(':') is ["queue", var context, var queue] && Parse(context, queue) is { } identity)
                found.Add(identity);
        foreach (var row in tile.Rows.Where(r => r.Details.GetValueOrDefault("source") == "queue"))
            if (Parse(row.Details.GetValueOrDefault("contextid"), row.Details.GetValueOrDefault("queueid")) is { } identity)
                found.Add(identity);
        return found.Count == 1 ? found.Single() : null;
    }

    private static QueueResourceIdentity? Parse(string? context, string? queue) =>
        Guid.TryParse(context, out var owner) && owner != Guid.Empty && Guid.TryParse(queue, out var id) && id != Guid.Empty
            ? new(owner, id) : null;
}
