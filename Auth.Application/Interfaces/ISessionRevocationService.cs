namespace Auth.Application.Interfaces;

/// <summary>
/// Application-layer façade that revokes every active session and refresh
/// token for a user as one logical operation. Callers stage the revocation
/// inside their current unit of work; the flush happens via
/// <c>IAuthUnitOfWork.SaveChangesAsync</c> (or the ambient transactional
/// executor) so the credential mutation and the session/token tear-down
/// commit together.
/// <para>
/// Phase 1 scope — wired into:
/// <list type="bullet">
///   <item><description><c>ResetPasswordCommandHandler</c> (self-service recovery)</description></item>
///   <item><description><c>AcceptInviteCommandHandler</c> (defensive; no-op pre-activation today)</description></item>
/// </list>
/// Phase 2+ call sites: admin reset, reassignment, suspend, archive.
/// </para>
/// </summary>
public interface ISessionRevocationService
{
    /// <summary>
    /// Stages revocation of all active sessions and refresh tokens for the
    /// user. Does NOT call SaveChanges — the caller's unit of work owns the
    /// commit boundary.
    /// </summary>
    /// <returns>
    /// <see cref="SessionRevocationOutcome"/> describing how many rows were
    /// transitioned. Useful for audit logging downstream.
    /// </returns>
    Task<SessionRevocationOutcome> RevokeAllForUserAsync(
        Guid userId,
        SessionRevocationReason reason,
        CancellationToken cancellationToken = default);
}

public sealed record SessionRevocationOutcome(
    int SessionsRevoked,
    int RefreshTokensRevoked);

/// <summary>
/// Why sessions were torn down. Recorded for audit; Phase 2 will persist
/// this reason against a lifecycle audit log.
/// </summary>
public enum SessionRevocationReason
{
    /// <summary>User completed a self-service password reset.</summary>
    PasswordReplacedBySelf = 1,

    /// <summary>Account was just activated (tears down any stray state — defensive).</summary>
    AccountActivated = 2,

    // Reserved for Phase 2+:
    // PasswordReplacedByAdmin  = 3,
    // AccountSuspended         = 4,
    // AccountReassigned        = 5,
    // AccountArchived          = 6,
}
