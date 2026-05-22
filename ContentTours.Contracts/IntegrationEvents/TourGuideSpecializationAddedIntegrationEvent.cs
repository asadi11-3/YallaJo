using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideSpecializationAddedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid SpecializationId
) : IntegrationEventBase;
