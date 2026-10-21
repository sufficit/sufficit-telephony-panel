namespace Sufficit.Telephony.Panel.Operations;

public sealed record QueueObservation(QueueBoardSample Source, QueueBoardQueue Queue, bool Fresh);
public sealed record QueueAgentObservation(QueueBoardSample Source, QueueAgentPresence Agent, bool Fresh);
public sealed record QueueVisitObservation(QueueBoardSample Source, QueueBoardVisit Visit, string QueueTitle, bool Fresh);
public sealed record QueueOverview(Guid ContextId, string QueueId, QueueObservation[] Observations, bool Partial)
{
    public string Title => Observations.OrderByDescending(o => o.Fresh).First().Queue.Title;
    public string Count(Func<QueueBoardQueue, int> value)
    {
        var current = Observations.Where(o => o.Fresh).ToArray();
        return current.Length == 0 ? "—" : (Partial ? "≥ " : "") + current.Sum(o => (long)Math.Max(0, value(o.Queue)));
    }
    // Capacity is catalog configuration, not additive across replicas.
    public string Capacity => CapacityText(static text => text);
    public string CapacityText(Func<string,string> text) => string.Join(" / ", Observations.Select(o => o.Queue.Capacity).Distinct().Order().Select(c => c == 0 ? text("Unlimited") : c.ToString()));
    public DateTimeOffset? OldestEnteredAt => Observations.Where(o => o.Fresh).Select(o => o.Queue.OldestEnteredAt).Min();
}
public sealed record QueueAgentOverview(Guid ContextId, string Extension, QueueAgentObservation[] Observations)
{
    public string Key => ContextId.ToString("N") + ":" + Extension;
    public string[] WatchedNodes => Observations.Where(o => o.Agent.Monitored).Select(o => o.Source.Node).Distinct().ToArray();
    public string[] UnwatchedNodes => Observations.Where(o => !o.Agent.Monitored).Select(o => o.Source.Node).Distinct().ToArray();
    public string State(DateTimeOffset now)
    {
        var states = Observations.Where(o => o.Fresh && o.Agent.Fresh(now)).Select(o => o.Agent.State(now, true)).Distinct().ToArray();
        if (states.Length == 0) return "No current data";
        // Do not invent a globally available endpoint from conflicting node readings.
        return states.Length == 1 ? states[0] : "Different readings";
    }
    public QueueContact[] Contacts => Observations.OrderByDescending(o => o.Fresh)
        .SelectMany(o => o.Agent.Contacts ?? []).Distinct().ToArray();
    public DateTimeOffset ObservedAt => Observations.Max(o => o.Agent.ObservedAt);
}

/// <summary>Presentation aggregation only. Scope and source identity remain intact.</summary>
public sealed record QueueMonitorOverview(QueueOverview[] Queues, QueueAgentOverview[] Agents, QueueVisitObservation[] Visits)
{
    public static QueueMonitorOverview Build(IEnumerable<QueueBoardSample> input, DateTimeOffset now, string search = "", string queueId = "")
    {
        // A configured source must not be counted twice if supplied more than once.
        var sources = input.GroupBy(s => (s.ContextId, NodeKey(s)))
            .Select(g => g.OrderByDescending(s => s.Snapshot?.ObservedAt).First()).ToArray();
        bool Fresh(QueueBoardSample s) => s.Available && s.Snapshot?.Fresh(now) == true;
        bool Matches(string? value) => search.Length == 0 || value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
        var queues = sources.SelectMany(s => (s.Snapshot?.Queues ?? []).Select(q => new QueueObservation(s, q, Fresh(s))))
            .GroupBy(o => (o.Source.ContextId, QueueId: Id(o.Queue.QueueId)))
            .Select(g => new QueueOverview(g.Key.ContextId, g.Key.QueueId, g.ToArray(), sources.Any(s => s.ContextId == g.Key.ContextId && !Fresh(s))))
            .Where(q => (queueId.Length == 0 || Id(queueId) == q.QueueId) && q.Observations.Any(o => Matches(o.Queue.Title + " " + o.Queue.QueueId)))
            .OrderBy(q => q.Title, StringComparer.OrdinalIgnoreCase).ThenBy(q => q.ContextId).ToArray();
        var agents = sources.SelectMany(s => (s.Snapshot?.Agents ?? []).Select(a => new QueueAgentObservation(s, a, Fresh(s))))
            .GroupBy(o => (o.Source.ContextId, o.Agent.Extension))
            .Select(g => new QueueAgentOverview(g.Key.ContextId, g.Key.Extension, g.OrderBy(o => o.Source.Node).ToArray()))
            .Where(a => Matches(a.Extension + " " + string.Join(" ", a.Contacts.Select(c => c.IpAddress + " " + c.UserAgent))))
            .OrderBy(a => a.Extension, StringComparer.OrdinalIgnoreCase).ThenBy(a => a.ContextId).ToArray();
        var visits = sources.SelectMany(s =>
        {
            var titles = (s.Snapshot?.Queues ?? []).ToDictionary(q => q.QueueId, q => q.Title);
            return (s.Snapshot?.Visits ?? []).Select(v => new QueueVisitObservation(s, v,
                titles.GetValueOrDefault(v.QueueId) ?? v.QueueId, Fresh(s)));
        })
            .Where(v => (queueId.Length == 0 || Id(v.Visit.QueueId) == Id(queueId)) && Matches(v.QueueTitle + " " + v.Visit.Caller + " " + v.Visit.ChannelName))
            .OrderByDescending(v => v.Visit.EnteredAt).ToArray();
        return new(queues, agents, visits);
    }
    private static string Id(string value) => Guid.TryParse(value, out var id) ? id.ToString("N") : value;
    private static string NodeKey(QueueBoardSample sample) => sample.NodeId != Guid.Empty ? sample.NodeId.ToString("N") : Id(sample.Snapshot?.NodeId ?? sample.Node);
}
