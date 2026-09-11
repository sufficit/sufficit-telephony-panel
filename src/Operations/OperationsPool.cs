using System.Security.Claims;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

public sealed class OperationsPool(PanelSessions sessions, SharedTelemetry telemetry) : BackgroundService
{
    private readonly Dictionary<string, OperationsConnection> connections = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<OperationsConnection> Get(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (await sessions.GetAccessTokenAsync(principal, ct) is null)
            throw new UnauthorizedAccessException("Sua sessão expirou ou perdeu a permissão manager. Entre novamente.");
        var key = principal.FindFirstValue("panel_session")!;
        await gate.WaitAsync(ct);
        try
        {
            if (!connections.TryGetValue(key, out var connection))
            {
                if (connections.Count >= 16) throw new InvalidOperationException("O limite de 16 sessões de monitoramento foi atingido. Feche painéis inativos e tente novamente em dois minutos.");
                connection = new OperationsConnection(principal, sessions, telemetry.Enabled ? telemetry : null);
                connections.Add(key, connection);
            }
            connection.Touch();
            return connection;
        }
        finally { gate.Release(); }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                KeyValuePair<string, OperationsConnection>[] snapshot;
                await gate.WaitAsync(stoppingToken);
                try { snapshot = connections.ToArray(); } finally { gate.Release(); }
                foreach (var pair in snapshot)
                {
                    if (DateTimeOffset.UtcNow - pair.Value.LastUse < TimeSpan.FromMinutes(2)
                        && await sessions.IsManagerAsync(pair.Value.Principal, stoppingToken)) continue;
                    await gate.WaitAsync(stoppingToken);
                    try { connections.Remove(pair.Key); } finally { gate.Release(); }
                    await pair.Value.DisposeAsync();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            foreach (var connection in connections.Values) await connection.DisposeAsync();
            connections.Clear();
        }
    }
}
