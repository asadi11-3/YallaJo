using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

/// <summary>
/// Raised when an admin rejects a submitted tour package.
/// </summary>
public sealed record TourPackageRejectedDomainEvent(
    Guid PackageId,
    Guid CreatedByUserId,
    string Reason) : DomainEventBase;
