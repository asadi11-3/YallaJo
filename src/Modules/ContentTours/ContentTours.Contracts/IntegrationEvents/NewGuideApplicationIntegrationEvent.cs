using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record NewGuideApplicationIntegrationEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid TourOwnerUserId,
    Guid TourId) : IntegrationEventBase;
