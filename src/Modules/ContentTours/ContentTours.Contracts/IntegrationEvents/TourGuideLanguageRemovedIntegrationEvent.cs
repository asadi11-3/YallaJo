using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideLanguageRemovedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid LanguageId
) : IntegrationEventBase;
