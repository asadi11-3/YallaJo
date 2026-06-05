namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Configurable retention windows for background cleanup of the Auth
/// module's disposable state. Owned by <see cref="AuthRetentionWorker"/>;
/// bound from configuration via <c>Auth:Retention</c>.
/// <para>
/// Phase 2C-4 introduced the worker with a security-first posture for
/// processed outbox rows. <b>SC-1/SC-2 hardening</b> adds explicit
/// retention windows for Sessions and RefreshTokens: previously these
/// were deleted immediately on expiry/revocation (zero retention),
/// which (a) removed any short-term security-review surface and
/// (b) silently weakened refresh-token reuse detection, since the
/// rotation flow relies on revoked tokens remaining queryable to catch
/// replays. The retention windows below keep those rows for a bounded
/// period before physical deletion.
/// </para>
/// </summary>
public sealed class AuthRetentionOptions
{
    public const string SectionName = "Auth:Retention";

    /// <summary>
    /// Master switch for the cleanup background worker. When
    /// <see langword="false"/> the hosted <see cref="AuthCleanupService"/>
    /// loop exits immediately without doing any work.
    /// <para>
    /// Used to ensure cleanup runs from a single host only. The API host
    /// sets this <see langword="true"/>; the Web host (and test hosts)
    /// set it <see langword="false"/>. ExecuteDeleteAsync is idempotent,
    /// but running the sweep from one host avoids duplicate work.
    /// </para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval (hours) between retention cycles. Replaces the previously
    /// hardcoded 24-hour loop so ops can tune cadence per environment.
    /// Clamped to a 1-hour floor by the hosted service.
    /// </summary>
    public int RunIntervalHours { get; set; } = 24;

    /// <summary>
    /// Days to retain <b>expired</b> (but not revoked) Sessions past their
    /// <c>ExpiresAt</c> before physical deletion. Default: 30 days for
    /// short-term security review.
    /// </summary>
    public int ExpiredSessionRetentionDays { get; set; } = 30;

    /// <summary>
    /// Days to retain <b>revoked</b> Sessions past their <c>RevokedAt</c>
    /// before physical deletion. Default: 30 days.
    /// </summary>
    public int RevokedSessionRetentionDays { get; set; } = 30;

    /// <summary>
    /// Days to retain <b>expired</b> (but not revoked) RefreshTokens past
    /// their <c>ExpiresAt</c> before physical deletion. Default: 30 days.
    /// Should be &gt;= the refresh-token lifetime (currently 30 days).
    /// </summary>
    public int ExpiredRefreshTokenRetentionDays { get; set; } = 30;

    /// <summary>
    /// Days to retain <b>revoked</b> RefreshTokens past their
    /// <c>RevokedAt</c> before physical deletion. Default: 30 days.
    /// <para>
    /// <b>Security-critical:</b> the refresh-token rotation flow
    /// (<c>RefreshTokenCommandHandler</c>) detects token reuse by finding
    /// an already-revoked token still in the table. Deleting revoked
    /// tokens too early turns a detectable replay into a generic
    /// "invalid token" response. Keep this &gt;= the window over which a
    /// stolen-then-rotated token could plausibly be replayed (the token
    /// lifetime, 30 days).
    /// </para>
    /// </summary>
    public int RevokedRefreshTokenRetentionDays { get; set; } = 30;

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
    /// Upper bound on how many rows the worker will delete per category
    /// per cycle. Deletion is performed in batches of this size (looping
    /// until a batch deletes fewer than the cap) so a large backlog does
    /// not lock a table with one giant DELETE. Kept as an option so ops
    /// can lower it if table locks become a concern on a hot instance.
    /// </summary>
    public int MaxDeletesPerCycle { get; set; } = 50_000;
}
