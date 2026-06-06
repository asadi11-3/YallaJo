using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

/// <summary>
/// Raised when an admin approves a submitted tour package.
/// </summary>
public sealed record TourPackageApprovedDomainEvent(
    Guid PackageId,
    Guid CreatedByUserId,
    Guid ApprovedByAdminId) : DomainEventBase;
