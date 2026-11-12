namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Explicit queue identity, including retained canonical keys. Never infer GUIDs from legacy numbers.</summary>
public sealed record QueueResourceIdentity(Guid ContextId, Guid QueueId)
{
    public string ManagementUrl(string root) => root.TrimEnd('/') +
        $"/pages/telephony/queue/edit?contextid={ContextId:N}&queueid={QueueId:N}";

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
