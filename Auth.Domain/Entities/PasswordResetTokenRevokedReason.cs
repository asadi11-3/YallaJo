namespace Auth.Domain.Entities;

/// <summary>
/// Why a <see cref="PasswordResetToken"/> was moved to
/// <see cref="PasswordResetTokenState.Revoked"/>. Enumerated so the
/// admin UI and the audit log can distinguish operational revocations
/// (email delivery failed) from intent-driven ones (admin manually
/// cancelled, or a newer send superseded this one).
/// <para>
/// Phase 2C-2 wires only the reasons needed by the self-service
/// <c>ForgotPassword</c> pipeline. Further reasons (e.g. revoke-on-
/// admin-reset or revoke-on-suspend) will be added when those admin
/// flows are implemented.
/// </para>
/// </summary>
public enum PasswordResetTokenRevokedReason
{
    /// <summary>
    /// Not revoked. Sentinel value for persisted rows whose
    /// <see cref="PasswordResetToken.RevokedAt"/> is null.
    /// </summary>
    None = 0,

    /// <summary>
    /// A newer <see cref="PasswordResetToken"/> was issued for the same
    /// user. Prior tokens are revoked before the new one is persisted
    /// so that at most one non-terminal token exists per user at any
    /// moment.
    /// </summary>
    Superseded = 1,

    /// <summary>
    /// An admin explicitly revoked the outstanding reset token. The
    /// verb is present on the aggregate today so the state machine is
    /// complete; admin revocation UI arrives in a later phase.
    /// </summary>
    AdminRevoked = 2,

    /// <summary>
    /// The reset email failed to leave the SMTP relay. The token was
    /// auto-revoked in the same unit of work so the row is not usable
    /// and the operator can audit which sends have failed.
    /// </summary>
    EmailFailed = 3,
}
