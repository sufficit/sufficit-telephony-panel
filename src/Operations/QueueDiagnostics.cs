using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace Sufficit.Telephony.Panel.Operations;
public sealed record QueueDiagnosticEvent(long Id, DateTimeOffset OccurredAt, string Kind, string? Extension, string? Reason);
public sealed record QueueDiagnosticHealth(long Accepted, long Written, long Dropped, int Pending, bool StorageAvailable);
public sealed record QueueDiagnosticEvents(Guid ContextId, Guid NodeId, Guid VisitId, QueueDiagnosticEvent[] Items, long? NextCursor, QueueDiagnosticHealth Health);
public sealed record QueueDiagnosticCall(Guid VisitId, Guid QueueId, string ChannelId, string Status, DateTimeOffset EnteredAt, DateTimeOffset? AnsweredAt, DateTimeOffset? EndedAt);
public sealed record QueueDiagnosticCalls(Guid ContextId, Guid NodeId, QueueDiagnosticCall[] Items, string? NextCursor);
public sealed record QueueDiagnosticQueueReport(Guid QueueId, long Entered, long Answered, long Abandoned, long TimedOut, decimal? AverageWaitSeconds, long AnsweredWithinSla, int SlaSeconds);
public sealed record QueueDiagnosticReport(Guid ContextId, Guid NodeId, QueueDiagnosticQueueReport[] Items);

/// <summary>Server-only bounded, shared single-flight history cache. No polling per call or row.</summary>
public sealed class QueueDiagnosticsReader(QueueBoardReader sources, IHttpClientFactory clients, TimeProvider clock) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1);
    private readonly Dictionary<string, (DateTimeOffset At, object Value)> cache = [];
    public async Task<T> Read<T>(Guid context, Guid node, string endpoint, CancellationToken ct) where T : class
    {
        var source = sources.DiagnosticSource(context, node) ?? throw new InvalidOperationException("Diagnostic source is not configured.");
        if (endpoint.Length > 512 || !System.Text.RegularExpressions.Regex.IsMatch(endpoint, "^(calls(/[a-f0-9]{32}/events)?|report)(\\?.*)?$")) throw new ArgumentException("Invalid diagnostic route.");
        var key = $"{context:N}:{node:N}:{endpoint}";
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct); budget.CancelAfter(TimeSpan.FromSeconds(5));
        await gate.WaitAsync(budget.Token);
        try
        {
            if (cache.TryGetValue(key, out var cached) && clock.GetUtcNow() - cached.At < TimeSpan.FromSeconds(endpoint.StartsWith("report") ? 30 : 2)) return (T)cached.Value;
            var info = new FileInfo(source.KeyFile);
            if (!info.Exists || info.Length is < 32 or > 4096 || !OperatingSystem.IsWindows() &&
                (File.GetUnixFileMode(info.FullName) & (UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.GroupWrite)) != 0) throw new InvalidDataException("Private diagnostic key required.");
            var token = (await File.ReadAllTextAsync(info.FullName, budget.Token)).Trim();
            if (token.Length is < 32 or > 256 || token.Any(char.IsWhiteSpace)) throw new InvalidDataException("Invalid diagnostic key.");
            using var request = new HttpRequestMessage(HttpMethod.Get, source.BaseUrl.TrimEnd('/') + $"/api/queue/contexts/{context:N}/nodes/{node:N}/" + endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await clients.CreateClient("queue-board").SendAsync(request, budget.Token);
            if (response.StatusCode is HttpStatusCode.NotImplemented or HttpStatusCode.NotFound) throw new InvalidOperationException("Call diagnostics are not enabled on this source.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { cache.Clear(); throw new UnauthorizedAccessException("Diagnostic source refused access."); }
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(budget.Token);
            if (json.Length > 512 * 1024) throw new InvalidDataException("Diagnostic response exceeded limit.");
            var value = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Invalid diagnostic page.");
            if (value is QueueDiagnosticEvents eventPage && endpoint.StartsWith("calls/") &&
                Guid.TryParseExact(endpoint.Split('/')[1], "N", out var requestedVisit) && eventPage.VisitId != requestedVisit)
                throw new InvalidDataException("Diagnostic visit mismatch.");
            if (!Valid(value, context, node)) throw new InvalidDataException("Diagnostic scope or shape mismatch.");
            if (cache.Count >= 128) cache.Remove(cache.MinBy(p => p.Value.At).Key);
            cache[key] = (clock.GetUtcNow(), value);
            return value;
        }
        finally { gate.Release(); }
    }
    public static bool Valid(object value, Guid context, Guid node) => value switch
    {
        QueueDiagnosticEvents e => e.ContextId == context && e.NodeId == node && e.VisitId != Guid.Empty && e.Items is { Length: <= 100 } &&
            e.Items.All(i => i is not null && i.Id > 0 && i.Kind is {Length: >0 and <=32} && i.Extension?.Length is not > 128 && i.Reason?.Length is not > 64) && e.Items.Select(i=>i.Id).SequenceEqual(e.Items.Select(i=>i.Id).Distinct().Order()) &&
            (e.NextCursor is null || e.Items.Length>0 && e.NextCursor==e.Items[^1].Id) &&
            e.Health is not null && e.Health.Dropped >= 0 && e.Health.Accepted>=0 && e.Health.Written>=0 && e.Health.Pending>=0,
        QueueDiagnosticCalls c => c.ContextId == context && c.NodeId == node && c.Items is { Length: <= 100 } && c.NextCursor?.Length is not > 80 &&
            c.Items.All(i => i is not null && i.VisitId != Guid.Empty && i.QueueId != Guid.Empty && i.ChannelId is {Length: >0 and <=150} && i.Status is {Length: >0 and <=24}),
        QueueDiagnosticReport r => r.ContextId == context && r.NodeId == node && r.Items is { Length: <= 2000 } &&
            r.Items.All(i => i is not null && i.QueueId != Guid.Empty && i.Entered >= 0 && i.Answered >= 0 && i.Answered <= i.Entered && i.AnsweredWithinSla>=0 && i.AnsweredWithinSla <= i.Answered && i.Abandoned>=0 && i.Abandoned<=i.Entered && i.TimedOut>=0 && i.TimedOut<=i.Entered && (i.AverageWaitSeconds is null || i.AverageWaitSeconds>=0) && i.SlaSeconds is >=1 and <=300),
        _ => false
    };
    public void Dispose() => gate.Dispose();
}

public sealed record QueueAccounting(Guid ContextId,string RecordId,string Node,string Status,uint? RecordedSeconds,
    decimal? TariffReferenceSeconds,decimal? Value,decimal? Debited);
