using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway reports a failure for a previously approved payout.
/// The platform operator must intervene or wait for the next batch.
/// </summary>
/// <param name="PayoutId">Identifier of the failed payout batch.</param>
/// <param name="ProviderId">Identifier of the receiving provider.</param>
/// <param name="FailureReason">Free-form failure reason from the gateway.</param>
/// <param name="RetriedAt">UTC instant when the failure was registered.</param>
public sealed record PayoutFailedDomainEvent(
    Guid PayoutId,
    Guid ProviderId,
    string FailureReason,
    DateTime RetriedAt) : DomainEventBase;
