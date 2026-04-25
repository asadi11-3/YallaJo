namespace Auth.Domain.Entities;

public enum ActivationTokenRevokedReason
{
    /// <summary>
    /// Not revoked. Sentinel value for persisted rows whose
    /// <see cref="ActivationToken.RevokedAt"/> is null.
    /// </summary>
    None = 0,

    /// <summary>
    /// A newer <see cref="ActivationToken"/> was issued for the same user
    /// (resend flow). Prior tokens are revoked before the new one is
    /// persisted so that at most one non-terminal token exists per user
    /// at any moment.
    /// </summary>
    Superseded = 1,

    /// <summary>
    /// An admin explicitly revoked the outstanding activation token
    /// (Phase 2C+ admin UI; the verb is present on the aggregate today so
    /// the state machine is complete).
    /// </summary>
    AdminRevoked = 2,

    /// <summary>
    /// The activation email failed to leave the SMTP relay. The token was
    /// auto-revoked in the same unit of work so the row is not usable and
    /// the operator can audit which sends have failed.
    /// </summary>
    EmailFailed = 3,
}
