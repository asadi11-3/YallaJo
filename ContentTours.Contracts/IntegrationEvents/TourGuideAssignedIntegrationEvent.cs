using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

/// <summary>
/// Raised when a Tour Guide is assigned to a Tour.
/// Consumers: Messaging (notify guide they were assigned), Analytics (guide activity).
/// </summary>
public sealed record TourGuideAssignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    bool IsPrimary,
    Guid AssignedByUserId
) : IntegrationEventBase;
