namespace Auth.Domain.Entities;

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
