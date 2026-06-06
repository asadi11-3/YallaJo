using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when an admin escalates an open dispute (e.g. to a senior support tier).
/// Mirrors <see cref="DisputeResolvedDomainEvent"/> but represents a non-final transition.
/// Phase-3 WS-4: introduces the missing domain event to complete the dispute lifecycle.
/// </summary>
public sealed record DisputeEscalatedDomainEvent(
    Guid DisputeId,
    Guid PaymentId,
    Guid UserId,
    string Reason,
    Guid EscalatedByAdminId) : DomainEventBase;
