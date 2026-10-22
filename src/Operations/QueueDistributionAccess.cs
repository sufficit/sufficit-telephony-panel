using Sufficit.Telephony.Panel.Security;
using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>
/// Tenant-scoped reads of the collector's queue distribution. The company filter is built here from the
/// current permissions and sent to the collector; an empty filter (every company) is reserved for managers.
/// </summary>
public sealed class QueueDistributionAccess(PanelAccess access, SharedTelemetry telemetry)
{
    public bool Enabled => telemetry.Enabled;

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
        var reply = await telemetry.Distribution(request, ct);
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
}
