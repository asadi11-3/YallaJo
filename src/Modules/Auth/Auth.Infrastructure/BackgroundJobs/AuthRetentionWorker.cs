using System.Linq.Expressions;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.BackgroundJobs;

/// <summary>
/// Phase 2C-4 — performs a single pass of disposable-state cleanup on
/// the Auth module. Hoisted out of <see cref="AuthCleanupService"/> so
/// the logic is unit-testable without the <c>BackgroundService</c>
/// harness.
/// <para>
/// Retention categories:
/// </para>
/// <list type="bullet">
///   <item><description><b>RefreshTokens</b>: revoked OR past <c>ExpiresAt</c>. Unchanged from pre-2C-4 policy.</description></item>
///   <item><description><b>Sessions</b>: revoked OR past <c>ExpiresAt</c>. Unchanged.</description></item>
///   <item><description><b>Otps</b>: <c>IsUsed</c> OR past <c>ExpiresAt</c>. Unchanged. The generic rule naturally drains any legacy <c>UserInvite</c> / <c>PasswordReset</c> fallback rows as activation tokens expire after 7 days and reset codes after 10 minutes; Phase 2C-4 adds no special-case here.</description></item>
///   <item><description><b>OutboxMessages</b>: processed rows older than <see cref="AuthRetentionOptions.ProcessedOutboxRetentionHours"/>. <b>New in Phase 2C-4</b> — addresses the Phase 2C-3 security tradeoff whereby plain activation tokens + reset codes travel inside <c>OutboxMessage.Content</c> JSON. Dead-lettered rows (retry-exhausted, still unprocessed) are NEVER auto-deleted — they remain in the table as a queryable ops surface.</description></item>
/// </list>
/// <para>
/// The delete primitive is injected as <see cref="IRetentionDeleteAdapter"/>
/// so tests can exercise the worker's cutoff computation and predicate
/// shape without requiring a provider that supports <c>ExecuteDeleteAsync</c>.
/// Production wiring uses <see cref="EfExecuteDeleteAdapter"/> which
/// maps straight to EF's <c>ExecuteDeleteAsync</c>.
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

        var refreshTokensDeleted = await deleteAdapter.DeleteAsync(
            dbContext.RefreshTokens,
            rt => !rt.IsDeleted && (rt.IsRevoked || rt.ExpiresAt < now),
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} expired/revoked RefreshTokens.",
            refreshTokensDeleted);

        var sessionsDeleted = await deleteAdapter.DeleteAsync(
            dbContext.Sessions,
            s => !s.IsDeleted && (s.IsRevoked || s.ExpiresAt < now),
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} expired/revoked Sessions.",
            sessionsDeleted);

        // Used / expired Otps — generic rule covers every purpose,
        // including legacy UserInvite / PasswordReset fallback rows.
        var otpsDeleted = await deleteAdapter.DeleteAsync(
            dbContext.Otps,
            o => !o.IsDeleted && (o.IsUsed || o.ExpiresAt < now),
            ct);

        logger.LogInformation(
            "AuthRetention: Deleted {Count} used/expired Otps (includes drainage of legacy UserInvite / PasswordReset fallback rows).",
            otpsDeleted);

        // Phase 2C-4 — purge processed outbox rows past their retention
        // window. The cutoff is ProcessedOnUtc (not OccurredOnUtc) so a
        // dead-lettered message (RetryCount exhausted, still
        // unprocessed) is never touched here.
        var retentionHours  = Math.Max(1, _opts.ProcessedOutboxRetentionHours);
        var processedCutoff = now.AddHours(-retentionHours);

        var processedOutboxDeleted = await deleteAdapter.DeleteAsync(
            dbContext.OutboxMessages,
            m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < processedCutoff,
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
}

/// <summary>
/// Thin seam over the EF <c>ExecuteDeleteAsync</c> primitive so the
/// retention worker's cutoff logic is unit-testable without a real
/// relational provider. Production wiring maps straight to EF; tests
/// supply an in-memory adapter.
/// </summary>
internal interface IRetentionDeleteAdapter
{
    Task<int> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct)
        where TEntity : class;
}

/// <summary>
/// Production adapter — calls <c>Where(...).ExecuteDeleteAsync</c>.
/// </summary>
internal sealed class EfExecuteDeleteAdapter : IRetentionDeleteAdapter
{
    public Task<int> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct)
        where TEntity : class
        => source.Where(predicate).ExecuteDeleteAsync(ct);
}
