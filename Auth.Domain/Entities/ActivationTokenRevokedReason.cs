namespace Auth.Domain.Entities;

/// <summary>
/// Why an <see cref="ActivationToken"/> was moved to
/// <see cref="ActivationTokenState.Revoked"/>. Enumerated so the admin UI
/// and the audit log can distinguish operational revocations (email
/// delivery failed) from intent-driven ones (admin manually cancelled, or
/// a newer send superseded this one).
/// <para>
/// Phase 2C-1 defines only the reasons needed by the <c>SendActivationEmail</c>
/// pipeline. Further reasons (e.g. <c>AccountArchived</c>) will be added
/// when the admin suspend/archive flows are wired in a later phase.
/// </para>
/// </summary>
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
