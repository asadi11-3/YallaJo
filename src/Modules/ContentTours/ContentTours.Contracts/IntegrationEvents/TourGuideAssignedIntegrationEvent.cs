using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideAssignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    bool IsPrimary,
    Guid AssignedByUserId
) : IntegrationEventBase;
