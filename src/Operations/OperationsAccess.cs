using Microsoft.AspNetCore.Components.Authorization;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Monitor;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>All upstream access remains server-side and acts as the current user.</summary>
public sealed class OperationsAccess(AuthenticationStateProvider authentication, PanelSessions sessions,
    OperationsPool pool, OperationsApi api, SupervisionPrefixPolicy prefixes)
{
    private async Task<string> Token(CancellationToken ct)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var token = await sessions.GetAccessTokenAsync(principal, ct)
            ?? throw new UnauthorizedAccessException("Sessão inválida. Entre novamente na Sufficit Identity.");
        if (!sessions.HasTelephoneScope(principal))
            throw new UnauthorizedAccessException("O login atual ainda não compartilha os direitos de telefonia. A equipe deve habilitar entitlements no cadastro deste painel; depois saia e entre novamente. A infraestrutura continua disponível.");
        return token;
    }
    public async Task<OperationsConnection> Connection(CancellationToken ct) =>
        await pool.Get((await authentication.GetAuthenticationStateAsync()).User, ct);
    public async Task<EventsPanelCardInfo[]> Cards(Guid? context, CancellationToken ct) =>
        await api.Cards(await Token(ct), context, ct);
    public async Task<(Guid Id, string Title)[]> Search(string text, CancellationToken ct)
    {
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
    public async Task Monitor(Guid context, string targetId, TelephonyMonitorActionMode mode, CancellationToken ct)
    {
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
