namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Phase 2C-4 — configurable retention windows for background cleanup
/// of the Auth module's disposable state. Owned by
/// <see cref="AuthRetentionWorker"/>; bound from configuration via
/// <c>Auth:Retention</c>.
/// <para>
/// The defaults prioritize security over breathing room: processed
/// outbox messages carry plain activation tokens / reset codes in
/// their payload (Phase 2C-3 Option A tradeoff), so the shorter we
/// can keep them, the narrower the at-rest exposure window.
/// </para>
/// </summary>
public sealed class AuthRetentionOptions
{
    public const string SectionName = "Auth:Retention";

    /// <summary>
    /// Age (hours) past which a successfully processed Auth outbox row
    /// becomes eligible for deletion. Default: 24 hours.
    /// <para>
    /// Dead-lettered rows (RetryCount &gt;= MaxRetryCount, still
    /// unprocessed) are NEVER auto-deleted by this worker; they remain
    /// in the table as a queryable ops surface until an operator
    /// explicitly intervenes.
    /// </para>
    /// </summary>
    public int ProcessedOutboxRetentionHours { get; set; } = 24;

    /// <summary>
    /// Upper bound on how many rows the worker will delete per
    /// ExecuteDeleteAsync call, expressed as a safety net (the actual
    /// delete is a single SQL statement — no streaming). Kept as an
    /// option so ops can lower it if table locks become a concern on
    /// a hot instance.
    /// </summary>
    public int MaxDeletesPerCycle { get; set; } = 50_000;
}
