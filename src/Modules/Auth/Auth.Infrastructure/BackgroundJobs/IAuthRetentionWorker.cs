namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Phase 2C-4 — abstraction over the per-cycle retention work. Split
/// from <see cref="AuthCleanupService"/> so the worker can be unit
/// tested against an in-memory DbContext without spinning up the
/// hosted <c>BackgroundService</c> loop.
/// </summary>
public interface IAuthRetentionWorker
{
    /// <summary>
    /// Executes one retention cycle synchronously. Returns the number
    /// of rows deleted per category so the caller (the hosted service)
    /// can emit structured metrics / logs.
    /// </summary>
    Task<AuthRetentionOutcome> ExecuteAsync(CancellationToken ct);
}

public sealed record AuthRetentionOutcome(
    int OtpsDeleted,
    int SessionsDeleted,
    int RefreshTokensDeleted,
    int ProcessedOutboxDeleted);
