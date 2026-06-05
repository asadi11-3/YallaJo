using System.Linq.Expressions;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Performs a single pass of disposable-state cleanup on the Auth
/// module. Hoisted out of <see cref="AuthCleanupService"/> so the logic
/// is unit-testable without the <c>BackgroundService</c> harness.
/// <para>
/// Retention categories:
/// </para>
/// <list type="bullet">
///   <item><description><b>RefreshTokens</b>: deleted only AFTER a retention window — revoked rows past
///   <see cref="AuthRetentionOptions.RevokedRefreshTokenRetentionDays"/> (measured on <c>RevokedAt</c>,
///   coalesced to <c>UpdatedAt</c>/<c>CreatedAt</c> for legacy null rows) OR expired rows past
///   <see cref="AuthRetentionOptions.ExpiredRefreshTokenRetentionDays"/> (measured on <c>ExpiresAt</c>).
///   <b>SC-1 hardening:</b> the previous policy deleted on revoke/expiry immediately, which weakened
///   reuse detection in <c>RefreshTokenCommandHandler</c> (a replayed revoked token must remain
///   queryable to be caught). The retention window preserves that surface.</description></item>
///   <item><description><b>Sessions</b>: deleted only AFTER a retention window — revoked rows past
///   <see cref="AuthRetentionOptions.RevokedSessionRetentionDays"/> OR expired rows past
///   <see cref="AuthRetentionOptions.ExpiredSessionRetentionDays"/>. Keeps a short-term security-review
///   surface instead of deleting on revoke/expiry.</description></item>
///   <item><description><b>Otps</b>: <c>IsUsed</c> OR past <c>ExpiresAt</c>. Unchanged — OTPs are
///   inherently short-lived. The generic rule naturally drains any legacy <c>UserInvite</c> /
///   <c>PasswordReset</c> fallback rows.</description></item>
///   <item><description><b>OutboxMessages</b>: processed rows older than
///   <see cref="AuthRetentionOptions.ProcessedOutboxRetentionHours"/>. Unchanged. Dead-lettered rows
///   (retry-exhausted, still unprocessed) are NEVER auto-deleted.</description></item>
/// </list>
/// <para>
/// Deletion runs in batches capped at <see cref="AuthRetentionOptions.MaxDeletesPerCycle"/> so a large
/// backlog does not lock a table with one giant DELETE. The delete primitive is injected as
/// <see cref="IRetentionDeleteAdapter"/> so tests can exercise the worker's cutoff computation and
/// predicate shape without a provider that supports <c>ExecuteDeleteAsync</c>.
/// </para>
/// </summary>
internal sealed class AuthRetentionWorker(
    AuthDbContext dbContext,
    IRetentionDeleteAdapter deleteAdapter,
    IOptions<AuthRetentionOptions> options,
    ILogger<AuthRetentionWorker> logger) : IAuthRetentionWorker
{
    private readonly AuthRetentionOptions _opts = options.Value;

    public async Task<AuthRetentionOutcome> ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, _opts.MaxDeletesPerCycle);

        // ── Retention cutoffs (UTC). Clamp negative configuration to 0 so a
        //    misconfiguration never widens the window into "delete recent".
        var expiredSessionCutoff = now.AddDays(-Math.Max(0, _opts.ExpiredSessionRetentionDays));
        var revokedSessionCutoff = now.AddDays(-Math.Max(0, _opts.RevokedSessionRetentionDays));
        var expiredRtCutoff      = now.AddDays(-Math.Max(0, _opts.ExpiredRefreshTokenRetentionDays));
        var revokedRtCutoff      = now.AddDays(-Math.Max(0, _opts.RevokedRefreshTokenRetentionDays));

        // ── RefreshTokens ──────────────────────────────────────────────────
        // Delete a revoked token only once it is past the revoked-retention
        // window (RevokedAt coalesced to UpdatedAt/CreatedAt for legacy rows
        // that predate RevokedAt being populated). Delete an expired-but-not-
        // revoked token only once past the expired-retention window.
        var refreshTokensDeleted = await DeleteInBatchesAsync(
            dbContext.RefreshTokens,
            rt => !rt.IsDeleted &&
                  ((rt.IsRevoked && (rt.RevokedAt ?? rt.UpdatedAt ?? rt.CreatedAt) < revokedRtCutoff)
                   || (!rt.IsRevoked && rt.ExpiresAt < expiredRtCutoff)),
            batchSize,
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} RefreshTokens past retention (revoked>{RevokedDays}d, expired>{ExpiredDays}d).",
            refreshTokensDeleted,
            _opts.RevokedRefreshTokenRetentionDays,
            _opts.ExpiredRefreshTokenRetentionDays);

        // ── Sessions ───────────────────────────────────────────────────────
        var sessionsDeleted = await DeleteInBatchesAsync(
            dbContext.Sessions,
            s => !s.IsDeleted &&
                 ((s.IsRevoked && (s.RevokedAt ?? s.UpdatedAt ?? s.CreatedAt) < revokedSessionCutoff)
                  || (!s.IsRevoked && s.ExpiresAt < expiredSessionCutoff)),
            batchSize,
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} Sessions past retention (revoked>{RevokedDays}d, expired>{ExpiredDays}d).",
            sessionsDeleted,
            _opts.RevokedSessionRetentionDays,
            _opts.ExpiredSessionRetentionDays);

        // ── Otps ───────────────────────────────────────────────────────────
        // Used / expired Otps — generic rule covers every purpose, including
        // legacy UserInvite / PasswordReset fallback rows. Unchanged.
        var otpsDeleted = await DeleteInBatchesAsync(
            dbContext.Otps,
            o => !o.IsDeleted && (o.IsUsed || o.ExpiresAt < now),
            batchSize,
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} used/expired Otps (includes drainage of legacy UserInvite / PasswordReset fallback rows).",
            otpsDeleted);

        // ── Processed OutboxMessages ───────────────────────────────────────
        // Purge processed outbox rows past their retention window. The cutoff
        // is ProcessedOnUtc (not OccurredOnUtc) so a dead-lettered message
        // (RetryCount exhausted, still unprocessed) is never touched here.
        var retentionHours  = Math.Max(1, _opts.ProcessedOutboxRetentionHours);
        var processedCutoff = now.AddHours(-retentionHours);

        var processedOutboxDeleted = await DeleteInBatchesAsync(
            dbContext.OutboxMessages,
            m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < processedCutoff,
            batchSize,
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} processed OutboxMessages older than {Hours}h (plain-token payload exposure mitigation).",
            processedOutboxDeleted,
            retentionHours);

        return new AuthRetentionOutcome(
            OtpsDeleted:             otpsDeleted,
            SessionsDeleted:         sessionsDeleted,
            RefreshTokensDeleted:    refreshTokensDeleted,
            ProcessedOutboxDeleted:  processedOutboxDeleted);
    }

    /// <summary>
    /// Repeatedly deletes up to <paramref name="batchSize"/> matching rows
    /// until a pass deletes fewer than the cap (i.e. the backlog is drained).
    /// Each batch is a single set-based delete; batching bounds the lock
    /// footprint of any one statement. The loop honours cancellation.
    /// </summary>
    private async Task<int> DeleteInBatchesAsync<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        int batchSize,
        CancellationToken ct)
        where TEntity : class
    {
        var total = 0;
        while (!ct.IsCancellationRequested)
        {
            var deleted = await deleteAdapter.DeleteAsync(source, predicate, batchSize, ct);
            total += deleted;
            if (deleted < batchSize)
                break;
        }

        return total;
    }
}

/// <summary>
/// Thin seam over the EF <c>ExecuteDeleteAsync</c> primitive so the
/// retention worker's cutoff logic is unit-testable without a real
/// relational provider. Production wiring maps straight to EF; tests
/// supply an in-memory adapter.
/// </summary>
internal interface IRetentionDeleteAdapter
{
    /// <summary>
    /// Deletes up to <paramref name="maxRows"/> rows matching
    /// <paramref name="predicate"/> and returns the number deleted.
    /// </summary>
    Task<int> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        int maxRows,
        CancellationToken ct)
        where TEntity : class;
}

/// <summary>
/// Production adapter — deletes a capped batch. EF Core's
/// <c>ExecuteDeleteAsync</c> does not support <c>Take</c> directly, so the
/// batch is expressed as a keyed sub-select: delete the rows whose primary
/// key is in the top-<paramref name="maxRows"/> matching set.
/// </summary>
internal sealed class EfExecuteDeleteAdapter : IRetentionDeleteAdapter
{
    public Task<int> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        int maxRows,
        CancellationToken ct)
        where TEntity : class
    {
        // All Auth entities deleted here derive from Entity (Guid Id key),
        // so a stable Id-keyed sub-select gives a deterministic, index-
        // friendly batch bound.
        var batchIds = source
            .Where(predicate)
            .OrderBy(e => EF.Property<Guid>(e, "Id"))
            .Select(e => EF.Property<Guid>(e, "Id"))
            .Take(maxRows);

        return source
            .Where(e => batchIds.Contains(EF.Property<Guid>(e, "Id")))
            .ExecuteDeleteAsync(ct);
    }
}
