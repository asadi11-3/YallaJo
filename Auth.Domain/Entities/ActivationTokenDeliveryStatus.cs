namespace Auth.Domain.Entities;

/// <summary>
/// Tracks the email-delivery side of an <see cref="ActivationToken"/>,
/// orthogonal to the token's own lifecycle <see cref="ActivationTokenState"/>.
/// <para>
/// Recorded on the aggregate so the admin UI (and future telemetry) can
/// answer "was the activation email actually sent?" without digging through
/// logs. Phase 2C-3 will promote this further when email moves to an
/// event-driven dispatcher with its own delivery record per attempt.
/// </para>
/// </summary>
public enum ActivationTokenDeliveryStatus
{
    /// <summary>
    /// Token row has been persisted but the email has not yet been
    /// attempted (e.g. the row exists momentarily between persistence and
    /// the <c>emailService.SendAsync</c> call).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Email was handed to the SMTP relay successfully. This is as much
    /// as the synchronous sender can know; actual inbox arrival is not
    /// observable here (Phase 2C-3 may add bounce tracking).
    /// </summary>
    Sent = 1,

    /// <summary>
    /// SMTP dispatch threw. The token is revoked in the same unit of work
    /// (<see cref="ActivationTokenRevokedReason.EmailFailed"/>) so the
    /// row cannot be used for activation and so the operator can audit
    /// the failure.
    /// </summary>
    Failed = 2,
}
