using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

/// <summary>
/// Raised when admin toggles IsFeatured on a Tour.
/// Only raised when the value actually changes (ERR-009 idempotency).
/// </summary>
public sealed record TourFeaturedChangedDomainEvent(
    Guid TourId,
    bool IsFeatured,
    Guid ChangedByUserId,
    DateTime ChangedAt) : DomainEventBase;
