namespace Sufficit.Telephony.Panel.Operations;

public sealed record AcdQueueObservation(AcdBoardSample Source, AcdBoardQueue Queue, bool Fresh);
public sealed record AcdAgentObservation(AcdBoardSample Source, AcdAgentPresence Agent, bool Fresh);
public sealed record AcdVisitObservation(AcdBoardSample Source, AcdBoardVisit Visit, string QueueTitle, bool Fresh);
public sealed record AcdQueueOverview(Guid ContextId, string QueueId, AcdQueueObservation[] Observations, bool Partial)
{
    public string Title => Observations.OrderByDescending(o => o.Fresh).First().Queue.Title;
    public string Count(Func<AcdBoardQueue, int> value)
    {
        var current = Observations.Where(o => o.Fresh).ToArray();
        return current.Length == 0 ? "—" : (Partial ? "≥ " : "") + current.Sum(o => (long)Math.Max(0, value(o.Queue)));
    }
    // Capacity is catalog configuration, not additive across replicas.
    public string Capacity => string.Join(" / ", Observations.Select(o => o.Queue.Capacity).Distinct().Order());
    public DateTimeOffset? OldestEnteredAt => Observations.Where(o => o.Fresh).Select(o => o.Queue.OldestEnteredAt).Min();
}
public sealed record AcdAgentOverview(Guid ContextId, string Extension, AcdAgentObservation[] Observations)
{
    public string Key => ContextId.ToString("N") + ":" + Extension;
    public string State(DateTimeOffset now)
    {
        var states = Observations.Where(o => o.Fresh && o.Agent.Fresh(now)).Select(o => o.Agent.State(now, true)).Distinct().ToArray();
        if (states.Length == 0) return "No current data";
        // Do not invent a globally available endpoint from conflicting node readings.
        return states.Length == 1 ? states[0] : "Different readings";
    }
    public AcdContact[] Contacts => Observations.OrderByDescending(o => o.Fresh)
        .SelectMany(o => o.Agent.Contacts ?? []).Distinct().ToArray();
    public DateTimeOffset ObservedAt => Observations.Max(o => o.Agent.ObservedAt);
}

/// <summary>Presentation aggregation only. Scope and source identity remain intact.</summary>
public sealed record AcdMonitorOverview(AcdQueueOverview[] Queues, AcdAgentOverview[] Agents, AcdVisitObservation[] Visits)
{
    public static AcdMonitorOverview Build(IEnumerable<AcdBoardSample> input, DateTimeOffset now, string search = "", string queueId = "")
    {
        // A configured source must not be counted twice if supplied more than once.
        var sources = input.GroupBy(s => (s.ContextId, NodeKey(s)))
            .Select(g => g.OrderByDescending(s => s.Snapshot?.ObservedAt).First()).ToArray();
        bool Fresh(AcdBoardSample s) => s.Available && s.Snapshot?.Fresh(now) == true;
        bool Matches(string? value) => search.Length == 0 || value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
        var queues = sources.SelectMany(s => (s.Snapshot?.Queues ?? []).Select(q => new AcdQueueObservation(s, q, Fresh(s))))
            .GroupBy(o => (o.Source.ContextId, QueueId: Id(o.Queue.QueueId)))
            .Select(g => new AcdQueueOverview(g.Key.ContextId, g.Key.QueueId, g.ToArray(), sources.Any(s => s.ContextId == g.Key.ContextId && !Fresh(s))))
            .Where(q => (queueId.Length == 0 || Id(queueId) == q.QueueId) && q.Observations.Any(o => Matches(o.Queue.Title + " " + o.Queue.QueueId)))
            .OrderBy(q => q.Title, StringComparer.OrdinalIgnoreCase).ThenBy(q => q.ContextId).ToArray();
        var agents = sources.SelectMany(s => (s.Snapshot?.Agents ?? []).Select(a => new AcdAgentObservation(s, a, Fresh(s))))
            .GroupBy(o => (o.Source.ContextId, o.Agent.Extension))
            .Select(g => new AcdAgentOverview(g.Key.ContextId, g.Key.Extension, g.OrderBy(o => o.Source.Node).ToArray()))
            .Where(a => Matches(a.Extension + " " + string.Join(" ", a.Contacts.Select(c => c.IpAddress + " " + c.UserAgent))))
            .OrderBy(a => a.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.ContextId).ToArray();
        var visits = sources.SelectMany(s => (s.Snapshot?.Visits ?? []).Select(v => new AcdVisitObservation(s, v,
                s.Snapshot?.Queues.FirstOrDefault(q => q.QueueId == v.QueueId)?.Title ?? v.QueueId, Fresh(s))))
            .Where(v => (queueId.Length == 0 || Id(v.Visit.QueueId) == Id(queueId)) && Matches(v.QueueTitle + " " + v.Visit.Caller + " " + v.Visit.ChannelName))
            .OrderByDescending(v => v.Visit.EnteredAt).ToArray();
        return new(queues, agents, visits);
    }
    private static string Id(string value) => Guid.TryParse(value, out var id) ? id.ToString("N") : value;
    private static string NodeKey(AcdBoardSample sample) => sample.NodeId != Guid.Empty ? sample.NodeId.ToString("N") : Id(sample.Snapshot?.NodeId ?? sample.Node);
}
