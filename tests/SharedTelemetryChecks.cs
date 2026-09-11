using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;
using Sufficit.Telephony.Streaming;

internal static class SharedTelemetryChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var directory = Directory.CreateTempSubdirectory("sufficit-shared-stream-");
        var path = Path.Combine(directory.FullName, "events.sock");
        var store = new TelemetryStore();
        void Append(string channel) => store.Append("eveo", "NewChannelEvent", JsonSerializer.SerializeToElement(new { channel }), DateTimeOffset.UtcNow);
        Append("PJSIP/first");
        var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.ListenUnixSocket(path, l => l.Protocols = HttpProtocols.Http2));
        builder.Services.AddSingleton(store); builder.Services.AddSingleton(new TelemetrySocketOptions(path)); builder.Services.AddGrpc();
        await using var app = builder.Build(); app.MapGrpcService<TelemetryGrpcService>(); await app.StartAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Telemetry:Socket"] = path }).Build();
        using var shared = new SharedTelemetry(config, NullLogger<SharedTelemetry>.Instance);
        using var sessions = new PanelSessions(new FakeHttp(), TimeProvider.System);
        using var pool = new OperationsPool(sessions, shared);
        try
        {
            await shared.StartAsync(default);
            await shared.WaitReady(default);
            check(shared.Projection.Snapshot(DateTimeOffset.UtcNow).Length == 1, "Shared backend receives initial snapshot");
            var principal = new ClaimsPrincipal();
            try { await pool.Get(principal, default); check(false, "Anonymous must not obtain shared telemetry"); }
            catch (UnauthorizedAccessException) { check(true, "Machine connection does not authorize anonymous users"); }
            await using var first = new OperationsConnection(principal, sessions, shared);
            await using var second = new OperationsConnection(principal, sessions, shared);
            await first.Connect(default); await second.Connect(default);
            check(ReferenceEquals(first.Projection, second.Projection), "Sessions reuse one machine projection");
            await first.DisposeAsync();
            Append("PJSIP/second");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (second.Projection.Snapshot(DateTimeOffset.UtcNow).Length != 2) await Task.Delay(20, deadline.Token);
            check(second.Connected, "Disposing a session cannot disconnect other managers");
            store.Reset(); Append("PJSIP/recovered");
            while (!second.Projection.Snapshot(DateTimeOffset.UtcNow).Any(x => x.Key == "PJSIP/recovered")) await Task.Delay(20, deadline.Token);
            check(second.Projection.Snapshot(DateTimeOffset.UtcNow).Length == 1, "Epoch reset atomically removes stale shared state");
        }
        finally
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await shared.StopAsync(stop.Token); await app.StopAsync(stop.Token); directory.Delete(true);
        }
    }
}
