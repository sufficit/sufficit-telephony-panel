using Sufficit.Telephony.Panel.Security;
using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Tenant-scoped reads of queue distribution from two sources: the collector (native app_queue queues) and
/// the Queue controller diagnostics (new queues). The company filter is built here from the current
/// permissions; an empty filter (every company) is reserved for managers.
/// </summary>
public sealed class QueueDistributionAccess(PanelAccess access, SharedTelemetry telemetry,
    QueueBoardAccess? queueBoard = null, ControllerQueueDistribution? controller = null)
{
    public bool Enabled => telemetry.Enabled || (queueBoard?.Configured == true && controller is not null);

    public async Task<DistributionReply> Read(Guid? context, string node, string queue, bool summary, CancellationToken ct, string search = "")
    {
        var permissions = await access.PermissionsAsync(ct);
        context = permissions.SelectContext(context);
        var request = new DistributionRequest { Node = node, Queue = queue, Summary = summary, Search = search.Length > 128 ? search[..128] : search };
        if (context is { } selected) request.Accounts.Add(selected.ToString("N"));
        else if (!permissions.Manager && !permissions.GlobalPanel)
        {
            // A customer without a selected context sees only its own companies, never unattributed queues.
            if (permissions.PanelContexts.Count == 0) throw new UnauthorizedAccessException("No authorized context.");
            foreach (var id in permissions.PanelContexts.Take(1000)) request.Accounts.Add(id.ToString("N"));
        }
        DistributionReply reply;
        InvalidOperationException? collectorError = null;
        if (telemetry.Enabled)
            try { reply = await telemetry.Distribution(request, ct); }
            catch (InvalidOperationException e) { collectorError = e; reply = new DistributionReply(); }
        else reply = new DistributionReply();
        var controllerQueues = await ControllerQueues(context, node, queue, search, ct);
        if (controllerQueues is not null) QueueDistributionView.Merge(reply, controllerQueues);
        else if (collectorError is not null) throw collectorError;
        permissions = await access.PermissionsAsync(ct);
        if (context is { } still && !permissions.CanRead(still)) throw new UnauthorizedAccessException("Context access expired.");
        // Defence in depth: drop anything the current permissions cannot read, even if the collector returned it.
        if (!permissions.Manager || context.HasValue)
        {
            bool Allowed(string account) => Guid.TryParse(account, out var id) && permissions.CanRead(id) && (!context.HasValue || id == context);
            var filtered = new DistributionReply { StartedAtMs = reply.StartedAtMs, NowMs = reply.NowMs };
            filtered.Queues.AddRange(reply.Queues.Where(q => Allowed(q.Account)));
            filtered.Members.AddRange(reply.Members.Where(m => Allowed(m.Account)));
            filtered.Calls.AddRange(reply.Calls.Where(c => Allowed(c.Account)));
            return filtered;
        }
        return reply;
    }

    /// <summary>New (Queue controller) queues the user can read: sources come from the permission-filtered queue board.</summary>
    private async Task<DistributionReply?> ControllerQueues(Guid? context, string node, string queue, string search, CancellationToken ct)
    {
        if (queueBoard is null || controller is null || !queueBoard.Configured) return null;
        try
        {
            var samples = await queueBoard.Read(context, ct);
            var result = new DistributionReply();
            foreach (var sample in samples.Where(s => s.NodeId != Guid.Empty && (node.Length == 0 || s.MatchesNode(node)))
                .GroupBy(s => (s.ContextId, s.NodeId)).Select(g => g.First()))
            {
                var titles = (sample.Snapshot?.Queues ?? []).Where(q => Guid.TryParse(q.QueueId, out _))
                    .GroupBy(q => Guid.Parse(q.QueueId)).ToDictionary(g => g.Key, g => g.First().Title);
                var data = await controller.Read(sample.ContextId, sample.NodeId, sample.Node, titles, ct);
                QueueDistributionView.Merge(result, QueueDistributionView.Filter(data, "", search.Trim(), queue));
            }
            return result;
        }
        catch (InvalidOperationException) { return null; }
        catch (HttpRequestException) { return null; }
    }
}
