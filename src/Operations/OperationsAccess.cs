using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.Panel;
using Sufficit.Telephony.Monitor;
using Sufficit.Telephony.Panel.Security;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>All upstream access remains server-side and acts as the current user.</summary>
public sealed class OperationsAccess(AuthenticationStateProvider authentication, PanelSessions sessions,
    OperationsPool pool, OperationsApi api, SupervisionPrefixPolicy prefixes, ILogger<OperationsAccess> logger)
{
    public async Task<Guid?> InitialContext(Guid? context, CancellationToken ct) =>
        await sessions.InitialContextAsync((await authentication.GetAuthenticationStateAsync()).User, context, ct);

    private readonly Dictionary<Guid, (DateTimeOffset Expires, EventsPanelCardInfo[] Cards)> catalogues = new();
    private async Task<EventsPanelCardInfo[]> Catalogue(Guid context, CancellationToken ct)
    {
        if (catalogues.TryGetValue(context, out var entry) && entry.Expires > DateTimeOffset.UtcNow) return entry.Cards;
        var cards = await Cards(context, ct);
        catalogues[context] = (DateTimeOffset.UtcNow.AddSeconds(60), cards);
        return cards;
    }

    private async Task<string> Token(CancellationToken ct)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var token = await sessions.GetAccessTokenAsync(principal, ct)
            ?? throw new UnauthorizedAccessException("Sessão inválida. Entre novamente na Sufficit Identity.");
        return token;
    }
    public async Task<QueueResourceIdentity?> QueueIdentity(string extension, Guid? context, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        context = permissions.SelectContext(context);
        var result = await api.QueueIdentity(await Token(ct), extension, context, ct);
        permissions = await Permissions(ct);
        permissions.SelectContext(context);
        if (result is not null && !permissions.CanRead(result.ContextId)) throw new UnauthorizedAccessException();
        return result;
    }
    public async Task<QueueAccounting> Accounting(Guid context,string node,string recordId,CancellationToken ct)
    {
        if(!(await Permissions(ct)).CanRead(context))throw new UnauthorizedAccessException();
        var result=await api.Accounting(await Token(ct),context,node,recordId,ct);
        if(!(await Permissions(ct)).CanRead(context))throw new UnauthorizedAccessException();
        return result;
    }
    public async Task<CurrentAuthorization> Permissions(CancellationToken ct) =>
        await sessions.AuthorizationAsync((await authentication.GetAuthenticationStateAsync()).User, ct);

    private readonly Dictionary<Guid, (DateTimeOffset Until, EndpointRegistration[] Rows)> registrations = new();
    private async Task<EndpointRegistration[]> Registrations(Guid? context, CancellationToken ct)
    {
        var key = context ?? Guid.Empty;
        if (registrations.TryGetValue(key, out var cached) && cached.Until > DateTimeOffset.UtcNow)
            return cached.Rows.Where(r => r.Current(DateTimeOffset.UtcNow)).ToArray();
        var rows = await api.Registrations(await Token(ct), context, ct);
        registrations[key] = (DateTimeOffset.UtcNow.AddSeconds(10), rows);
        return rows.Where(r => r.Current(DateTimeOffset.UtcNow)).ToArray();
    }
    public bool InboundUnavailable { get; private set; }
    private async Task<ObservedResource[]> InboundRows(Guid context, EventsPanelCardInfo[] cards, ObservedResource[] rows, CancellationToken ct)
    {
        if (!cards.Any(c => InboundCatalogueEntry.State(c.Channels) is not null && c.Channels.Single().StartsWith("entry:did:", StringComparison.Ordinal))) return rows;
        var sources = await api.Inbound(await Token(ct), context, ct);
        if (!(await Permissions(ct)).CanRead(context)) throw new UnauthorizedAccessException();
        var now = DateTimeOffset.UtcNow;
        InboundUnavailable |= sources.Any(s => !s.Fresh(now));
        return InboundCallSnapshot.Merge(rows, sources.Where(s => s.Fresh(now)).SelectMany(s => s.Calls).Where(c => c.Fresh(now)))
            .Concat(InboundCallSnapshot.IdleEvidence(cards, sources, context, now)).ToArray();
    }
    public async Task<AuthorizedObservation> Observation(Guid? context, EventsPanelCardInfo[] cards, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        context = permissions.SelectContext(context);
        var connection = await Connection(ct);
        var rows = connection.Connected ? connection.Projection.Snapshot(DateTimeOffset.UtcNow) : [];
        // Registration is a separate read-only snapshot: idle realtime PJSIP contacts may emit no AMI events.
        // Never store it in the shared stream or infer an account from a channel display number.
        if (context.HasValue || permissions.Manager)
        {
            var presence = await Registrations(context, ct);
            permissions = await Permissions(ct);
            context = permissions.SelectContext(context);
            if (!context.HasValue && !permissions.Manager) throw new UnauthorizedAccessException();
            presence = presence.Where(r => permissions.CanRead(r.ContextId)).ToArray();
            rows = EndpointRegistration.Merge(rows, presence, DateTimeOffset.UtcNow);
            if (context.HasValue) cards = EndpointRegistration.Attach(cards, presence);
        }
        InboundUnavailable = false;
        if (context is { } inboundContext) rows = await InboundRows(inboundContext, cards, rows, ct);
        if (permissions.Manager) rows = AuthorizedObservation.InheritCallAccounts(rows);
        if (permissions.Manager)
            return context is { } managerContext ? AuthorizedObservation.Create(cards, rows, managerContext)
                : AuthorizedObservation.ForManager(rows);
        if (context is null && !permissions.GlobalPanel)
        {
            foreach (var removed in catalogues.Keys.Where(id => !permissions.CanRead(id)).ToArray()) catalogues.Remove(removed);
            var scoped = new Dictionary<Guid, EventsPanelCardInfo[]>();
            var total = 0;
            foreach (var id in permissions.PanelContexts.Order())
            {
                var catalogue = await Catalogue(id, ct);
                total += catalogue.Length;
                if (total > 10000) throw new InvalidOperationException("Há mais de 10.000 recursos. Selecione uma empresa.");
                var presence = await Registrations(id, ct);
                rows = await InboundRows(id, catalogue, rows, ct);
                scoped[id] = EndpointRegistration.Attach(catalogue, presence);
                rows = EndpointRegistration.Merge(rows, presence, DateTimeOffset.UtcNow);
            }
            permissions = await Permissions(ct);
            return AuthorizedObservation.ForPortfolio(scoped, rows, permissions);
        }
        return AuthorizedObservation.ForCustomer(cards, rows, permissions, context);
    }
    public async Task<OperationsConnection> Connection(CancellationToken ct) =>
        await pool.Get((await authentication.GetAuthenticationStateAsync()).User, ct);
    public async Task<EventsPanelCardInfo[]> Cards(Guid? context, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        context = permissions.SelectContext(context);
        if (context is null) return [];
        var cards = await api.Cards(await Token(ct), context, ct);
        if (!(await Permissions(ct)).CanRead(context.Value)) throw new UnauthorizedAccessException();
        return cards;
    }
    public async Task<(Guid Id, string Title)[]> Search(string text, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        if (!permissions.PanelAllowed) throw new UnauthorizedAccessException();
        if (!permissions.GlobalPanel) return permissions.PanelContexts
            .Where(id => id.ToString("D").Contains(text, StringComparison.OrdinalIgnoreCase)
                || id.ToString("N").Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(20).Select(id => (id, id.ToString("D"))).ToArray();
        var json = await api.Search(await Token(ct), text, ct);
        if (json.ValueKind != System.Text.Json.JsonValueKind.Array) return [];
        return json.EnumerateArray().Take(20).Where(x => x.TryGetProperty("id", out var id) && id.TryGetGuid(out _))
            .Select(x => (x.GetProperty("id").GetGuid(), x.TryGetProperty("title", out var title) ? title.GetString() ?? "Sem nome" : "Sem nome")).ToArray();
    }
    public async Task<(Guid Id, string Title)?> Supervisor(CancellationToken ct) => await api.Supervisor(await Token(ct), ct);
    public Task MonitorPrefix(string ruleId, TelephonyMonitorActionMode mode, CancellationToken ct) =>
        MonitorPrefix(prefixes.Get(ruleId), mode, ct);

    public Task MonitorEndpointPrefix(EndpointSupervisionTarget target, TelephonyMonitorActionMode mode, CancellationToken ct)
    {
        // Candidate fields are not trusted: the shared path re-reads scoped API cards,
        // checks exact PEER identity and current observations before originating.
        if (target.ContextId == Guid.Empty || string.IsNullOrWhiteSpace(target.Node)
            || !SupervisionPrefixPolicy.SafePrefix(target.Prefix))
            throw new InvalidOperationException("Invalid supervision endpoint.");
        return MonitorPrefix(new SupervisionPrefixRule { Id = "endpoint", ContextId = target.ContextId,
            Node = target.Node, CardKey = target.PeerKey, Prefix = target.Prefix }, mode, ct, target);
    }

    private async Task MonitorPrefix(SupervisionPrefixRule rule, TelephonyMonitorActionMode mode, CancellationToken ct,
        EndpointSupervisionTarget? endpoint = null)
    {
        var permissions = await Permissions(ct);
        if (!permissions.CanRead(rule.ContextId) || !permissions.CanMonitor(rule.ContextId)) throw new UnauthorizedAccessException();
        if (mode is not (TelephonyMonitorActionMode.Listen or TelephonyMonitorActionMode.Whisper))
            throw new InvalidOperationException("Invalid supervision mode.");
        var connection = await Connection(ct);
        if (!await connection.ActionGate.WaitAsync(0, ct)) throw new InvalidOperationException("A request is already in progress.");
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - connection.LastAction < TimeSpan.FromSeconds(15)) throw new InvalidOperationException("Wait 15 seconds before starting another supervision request.");
            var token = await Token(ct);
            var cards = await api.Cards(token, rule.ContextId, ct);
            var supervisor = await api.Supervisor(token, ct) ?? throw new InvalidOperationException("Set your preferred extension in the portal to enable supervision.");
            var rows = connection.Projection.Snapshot(DateTimeOffset.UtcNow);
            if (!(endpoint is null ? SupervisionPrefixPolicy.Authorized(rule, cards) : endpoint.Authorized(cards))
                || !(endpoint is null ? SupervisionPrefixPolicy.Observed(rule, rows, connection.Connected, DateTimeOffset.UtcNow)
                    : endpoint.Observed(rows, connection.Connected, DateTimeOffset.UtcNow)))
                throw new InvalidOperationException("The prefix has no fresh authorized observation or spans conflicting contexts.");
            connection.LastAction = now;
            await api.Action(token, new TelephonyMonitorActionRequest
            {
                ContextId = rule.ContextId, Mode = mode, SupervisorExtension = "wss-" + supervisor.Id.ToString("N"),
                SupervisorProtocol = "PJSIP", SupervisorChannel = "PJSIP/wss-" + supervisor.Id.ToString("N"),
                TargetPeerKey = rule.Prefix, TargetChannelKey = null, Node = rule.Node, Timeout = 30000,
                CallerId = "Sufficit Monitor"
            }, ct);
        }
        finally { connection.ActionGate.Release(); }
    }
    /// <summary>Managers: continuous supervision of a shared trunk prefix (see <see cref="SharedSupervisionPrefix"/>).</summary>
    public async Task MonitorSharedPrefix(string node, string prefix, TelephonyMonitorActionMode mode, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        if (!permissions.Manager) throw new UnauthorizedAccessException();
        if (mode is not (TelephonyMonitorActionMode.Listen or TelephonyMonitorActionMode.Whisper)) throw new InvalidOperationException("Invalid supervision mode.");
        if (string.IsNullOrWhiteSpace(node) || !SupervisionPrefixPolicy.SafePrefix(prefix) || prefix.StartsWith("Local/", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid supervision prefix.");
        var connection = await Connection(ct);
        if (!await connection.ActionGate.WaitAsync(0, ct)) throw new InvalidOperationException("A request is already in progress.");
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - connection.LastAction < TimeSpan.FromSeconds(15)) throw new InvalidOperationException("Wait 15 seconds before starting another supervision request.");
            var token = await Token(ct);
            var supervisor = await api.Supervisor(token, ct) ?? throw new InvalidOperationException("Set your preferred extension in the portal to enable supervision.");
            var supervisorContext = await api.SupervisorContext(token, ct) ?? throw new InvalidOperationException("Your preferred supervision extension has no company.");
            var rows = connection.Connected ? connection.Projection.Snapshot(now) : [];
            var companies = SharedSupervisionPrefix.Companies(rows, node, prefix);
            if (companies.Length == 0) throw new InvalidOperationException("The prefix has no current observed call on this server.");
            connection.LastAction = now;
            logger.LogWarning("Shared-prefix supervision by manager {Subject}: mode={Mode} node={Node} prefix={Prefix} companies={Companies}",
                (await authentication.GetAuthenticationStateAsync()).User.FindFirst("sub")?.Value ?? "unknown", mode, node, prefix, string.Join(",", companies));
            await api.Action(token, new TelephonyMonitorActionRequest
            {
                ContextId = supervisorContext, Mode = mode, SupervisorExtension = "wss-" + supervisor.Id.ToString("N"),
                SupervisorProtocol = "PJSIP", SupervisorChannel = "PJSIP/wss-" + supervisor.Id.ToString("N"),
                TargetPeerKey = prefix, TargetChannelKey = null, Node = node, Timeout = 30000,
                CallerId = "Sufficit Monitor"
            }, ct);
        }
        finally { connection.ActionGate.Release(); }
    }
    public async Task Hangup(ObservedResource expected, CancellationToken ct)
    {
        var permissions=await Permissions(ct);
        if(!permissions.Manager) throw new UnauthorizedAccessException();
        var connection=await Connection(ct);
        if(!await connection.ActionGate.WaitAsync(0,ct)) throw new InvalidOperationException("A request is already in progress.");
        try {
            var row=connection.Projection.Snapshot(DateTimeOffset.UtcNow).FirstOrDefault(r=>r.Id==expected.Id && r.Node==expected.Node && r.Key==expected.Key);
            if(row is null || row.Node!=expected.Node || row.Key!=expected.Key
                || row.Details.GetValueOrDefault("uniqueid")!=expected.Details.GetValueOrDefault("uniqueid")
                || ChannelHangupGuard.BlockReason(row,permissions.Manager,connection.Connected) is not null)
                throw new InvalidOperationException("The selected channel ended or changed. Refresh before trying again.");
            await api.Hangup(await Token(ct),row,ct);
        }
        finally {connection.ActionGate.Release();}
    }
    public async Task Monitor(Guid context, string targetId, TelephonyMonitorActionMode mode, CancellationToken ct)
    {
        var permissions = await Permissions(ct);
        if (!permissions.CanRead(context) || !permissions.CanMonitor(context)) throw new UnauthorizedAccessException();
        if (mode is not (TelephonyMonitorActionMode.Listen or TelephonyMonitorActionMode.Whisper)) throw new InvalidOperationException("Modo de monitoramento inválido.");
        var connection = await Connection(ct);
        if (!await connection.ActionGate.WaitAsync(0, ct)) throw new InvalidOperationException("Uma solicitação já está em andamento.");
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - connection.LastAction < TimeSpan.FromSeconds(15)) throw new InvalidOperationException("Aguarde 15 segundos antes de iniciar outro monitoramento.");
            var token = await Token(ct);
            // Re-query tenant authorization before every mutation; UI selection is not authority.
            var cards = await api.Cards(token, context, ct);
            var supervisor = await api.Supervisor(token, ct) ?? throw new InvalidOperationException("Configure seu ramal preferido no portal antes de monitorar.");
            var row = connection.Projection.Snapshot(DateTimeOffset.UtcNow).FirstOrDefault(x => x.Id == targetId);
            if (row is null || !MonitorActionGuard.Allows(row, context, connection.Connected, DateTimeOffset.UtcNow)
                || !cards.Any(card => LiveProjection.Matches(card, row)))
                throw new InvalidOperationException("A chamada não tem confirmação recente, não pertence a este cliente ou já terminou. Aguarde um novo evento.");
            var channel = "PJSIP/wss-" + supervisor.Id.ToString("N");
            connection.LastAction = now;
            await api.Action(token, new TelephonyMonitorActionRequest
            {
                ContextId = context, Mode = mode, SupervisorExtension = "wss-" + supervisor.Id.ToString("N"),
                SupervisorProtocol = "PJSIP", SupervisorChannel = channel, TargetChannelKey = row.Key,
                TargetPeerKey = row.Key[..row.Key.LastIndexOf('-')], Node = row.Node, Timeout = 30000,
                CallerId = "Sufficit Monitor"
            }, ct);
        }
        finally { connection.ActionGate.Release(); }
    }
}
