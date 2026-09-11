using Grpc.Core;
using Sufficit.Telephony.Streaming;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>One read-only machine stream per backend instance, never per browser or user.</summary>
public sealed class SharedTelemetry(IConfiguration configuration, ILogger<SharedTelemetry> logger) : BackgroundService
{
    private readonly TelemetryReplica replica = new();
    private volatile bool connected;
    private long lastFrame;
    private long connectedAt;
    public bool Enabled => !string.IsNullOrWhiteSpace(configuration["Telemetry:Socket"]);
    public bool Connected => connected && DateTimeOffset.UtcNow - new DateTimeOffset(Interlocked.Read(ref lastFrame), TimeSpan.Zero) < TimeSpan.FromSeconds(25);
    public DateTimeOffset? ConnectedAt => Connected ? new DateTimeOffset(Interlocked.Read(ref connectedAt), TimeSpan.Zero) : null;
    public LiveProjection Projection => replica.Projection;
    public async Task WaitReady(CancellationToken ct)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(TimeSpan.FromSeconds(10));
        try { while (!Connected) await Task.Delay(50, limit.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new InvalidOperationException("O fluxo interno de telefonia ainda está sincronizando. Tente reconectar em alguns segundos."); }
    }
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (!Enabled) return;
        using var channel = UnixTelemetryChannel.Create(configuration["Telemetry:Socket"]!);
        var client = new Telemetry.TelemetryClient(channel);
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var call = client.Watch(new WatchRequest { Epoch = replica.Epoch, AfterSequence = replica.Sequence },
                    deadline: DateTime.UtcNow.AddMinutes(10), cancellationToken: stop);
                while (await call.ResponseStream.MoveNext(stop))
                {
                    replica.Apply(call.ResponseStream.Current);
                    Interlocked.Exchange(ref lastFrame, DateTimeOffset.UtcNow.UtcTicks);
                    if (!connected && replica.Ready) Interlocked.Exchange(ref connectedAt, (replica.StartedAt ?? DateTimeOffset.UtcNow).UtcTicks);
                    connected = replica.Ready;
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
            catch (RpcException e) when (stop.IsCancellationRequested && e.StatusCode == StatusCode.Cancelled) { break; }
            catch (InvalidDataException) { replica.Reset(); logger.LogWarning("Telemetry sequence mismatch; requesting fresh snapshot"); }
            catch (Exception e)
            {
                // Do not log exception messages, tokens, frames or server response bodies.
                logger.LogWarning("Internal telemetry disconnected: {Category}", e is RpcException rpc ? rpc.StatusCode.ToString() : e.GetType().Name);
            }
            finally { connected = false; replica.Disconnected(); }
            try { await Task.Delay(TimeSpan.FromSeconds(2 + Random.Shared.NextDouble() * 3), stop); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
        }
    }
}
