namespace Sufficit.Telephony.Panel.Operations;

public sealed record PanelDiagnostic(string Reference, DateTimeOffset OccurredAt, string Category);

/// <summary>Correlates visible failures without retaining exception messages, tokens or call payloads.</summary>
public sealed class PanelDiagnostics(ILogger<PanelDiagnostics> logger, TimeProvider clock)
{
    /// <summary>Supervision refusals raised by the panel itself are curated, user-safe reasons; show them as is.</summary>
    public static string SupervisionMessage(Exception error) => error is InvalidOperationException { Message.Length: > 0 and < 300 } refusal
        && error.GetType() == typeof(InvalidOperationException) ? refusal.Message : UserMessage(error);
    public static string UserMessage(Exception error) => error switch
    {
        UnauthorizedAccessException => "Your session or permissions no longer allow this operation. Sign in again or contact an administrator.",
        OperationCanceledException => "The query timed out or was interrupted. Try refreshing.",
        _ => "Could not query the telephony service. Try refreshing; if it persists, check the connection and permissions."
    };

    public PanelDiagnostic Report(Exception error, string operation)
    {
        var diagnostic = new PanelDiagnostic(Guid.CreateVersion7().ToString("N"), clock.GetUtcNow(), error.GetType().Name);
        logger.LogWarning("Panel failure {Reference}; operation={Operation}; category={Category}; stack={Stack}",
            diagnostic.Reference, operation, diagnostic.Category, error.StackTrace);
        return diagnostic;
    }
}
