using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

/// <summary>
/// Raised when a provider submits a draft tour package for admin review.
/// </summary>
public sealed record TourPackageSubmittedDomainEvent(
    Guid PackageId,
    Guid CreatedByUserId,
    string Name) : DomainEventBase;
