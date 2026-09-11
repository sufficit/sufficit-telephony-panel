using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>One bounded upstream stream per authenticated session, shared across tabs.</summary>
public sealed class OperationsConnection : IAsyncDisposable
{
    private readonly HubConnection? hub;
    private readonly SharedTelemetry? shared;
    private readonly LiveProjection projection = new();
    private DateTimeOffset? connectedAt;
    private readonly SemaphoreSlim start = new(1, 1);
    private long touched = DateTimeOffset.UtcNow.UtcTicks;
    public ClaimsPrincipal Principal { get; }
    public LiveProjection Projection => shared?.Projection ?? projection;
    public bool Connected => shared?.Connected ?? hub?.State == HubConnectionState.Connected;
    public DateTimeOffset LastUse => new(Interlocked.Read(ref touched), TimeSpan.Zero);
    public DateTimeOffset? ConnectedAt => shared is not null ? shared.ConnectedAt : connectedAt;
    public SemaphoreSlim ActionGate { get; } = new(1, 1);
    public DateTimeOffset LastAction { get; set; }

    public OperationsConnection(ClaimsPrincipal principal, PanelSessions sessions, SharedTelemetry? telemetry = null)
    {
        Principal = principal;
        shared = telemetry;
        if (shared is not null) return;
        hub = new HubConnectionBuilder().WithUrl("https://eveo-apps.sufficit.com.br:26507/amihub", options =>
        {
            options.AccessTokenProvider = () => sessions.GetAccessTokenAsync(Principal, CancellationToken.None);
            options.ApplicationMaxBufferSize = 128 * 1024;
            options.TransportMaxBufferSize = 128 * 1024;
            options.CloseTimeout = TimeSpan.FromSeconds(3);
        }).WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)])
          .Build();
        hub.ServerTimeout = TimeSpan.FromSeconds(40);
        foreach (var eventName in LiveProjection.Events)
            hub.On<string, JsonElement>(eventName, (node, payload) => Projection.Apply(node, eventName, payload, DateTimeOffset.UtcNow));
        hub.Reconnecting += _ => { Projection.Clear(); connectedAt = null; return Task.CompletedTask; };
        hub.Reconnected += _ => { Projection.Clear(); connectedAt = DateTimeOffset.UtcNow; return Task.CompletedTask; };
        hub.Closed += _ => { Projection.Clear(); connectedAt = null; return Task.CompletedTask; };
    }
    public void Touch() => Interlocked.Exchange(ref touched, DateTimeOffset.UtcNow.UtcTicks);
    public async Task Connect(CancellationToken ct)
    {
        Touch();
        if (shared is not null) { await shared.WaitReady(ct); return; }
        await start.WaitAsync(ct);
        try
        {
            if (hub!.State != HubConnectionState.Disconnected) return;
            Projection.Clear();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await hub.StartAsync(timeout.Token);
            connectedAt = DateTimeOffset.UtcNow;
        }
        finally { start.Release(); }
    }
    public async ValueTask DisposeAsync() { if (hub is not null) await hub.DisposeAsync(); }
}
