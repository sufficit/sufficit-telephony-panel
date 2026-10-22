using Microsoft.Extensions.Logging;
using Sufficit.Telephony.Panel.Operations;

static class PanelDiagnosticChecks
{
    public static void Run(Action<bool, string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var context = Guid.NewGuid();
        var row = new ObservedResource("id", "google", "call", "SIP/6007-000000ab", "Up", "", "", context.ToString("N"), now, now, new Dictionary<string, string>());
        check(MonitorActionGuard.BlockReason(row, context, true, now) is null, "Eligible channel has no blocking explanation");
        var cases = new[] {
            row with { Key = "Local/6007@demo-000000ab;1" },
            row with { Account = null }, row with { Account = Guid.Empty.ToString() },
            row with { Account = Guid.NewGuid().ToString() },
            row with { Updated = now.AddSeconds(1) }, row with { Node = "" }, row with { Kind = "peer" },
            row with { Key = "SIP/6007\r\nAction: Originate" }
        };
        foreach (var item in cases)
        {
            check(MonitorActionGuard.BlockReason(item, context, true, now) is not null, "Every refused channel has an explanation");
            check(!MonitorActionGuard.Allows(item, context, true, now), "Explaining a refused channel cannot authorize it");
        }
        check(MonitorActionGuard.Allows(row with { Updated = now.AddMinutes(-20) }, context, true, now), "An answered call stays eligible without new events");
        check(MonitorActionGuard.BlockReason(row, context, false, now) is not null, "Disconnected stream has an explanation");
        var snapshot = new QueueBoardSnapshot(context.ToString("N"), Guid.NewGuid().ToString("N"), now, true, false, []);
        var sample = new QueueBoardSample("google", context, snapshot, true);
        check(QueueSourceHealth.Problem(sample, now) is null, "Fresh source does not show a failure");
        foreach (var reading in new[] { snapshot, snapshot with { ControllerConnected = false }, snapshot with { ObserverOnly = true },
            snapshot with { ObservedAt = default }, snapshot with { ObservedAt = now.AddSeconds(-21) }, snapshot with { ObservedAt = now.AddSeconds(6) } })
            check((QueueSourceHealth.Problem(sample with { Snapshot = reading }, now) is null) == reading.Fresh(now), "Source explanations agree with freshness contract");
        check(QueueSourceHealth.Problem(sample with { Available = false }, now) is not null, "Failed query does not claim cached snapshot is current");
        var clock = new FakeClock { Now = now };
        var logger = new DiagnosticLogger();
        var diagnostics = new PanelDiagnostics(logger, clock);
        var exception = new InvalidOperationException("SECRET_TOKEN_AND_CALLER");
        var report = diagnostics.Report(exception, "test-operation");
        check(Guid.TryParseExact(report.Reference, "N", out _), "Failure receives a usable correlation reference");
        check(report.OccurredAt == now && logger.Last.Contains(report.Reference), "Screen reference and timestamp correlate to server log");
        check(!logger.Last.Contains("SECRET_TOKEN_AND_CALLER"), "Server diagnostic excludes exception messages");
        check(!PanelDiagnostics.UserMessage(exception).Contains("SECRET_TOKEN_AND_CALLER"), "User diagnostic excludes exception messages");
        check(diagnostics.Report(exception, "test-operation").Reference != report.Reference, "Separate failures receive separate references");
    }
    private sealed class DiagnosticLogger : ILogger<PanelDiagnostics>
    {
        public string Last = "";
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter) => Last = formatter(state, error);
    }
}
