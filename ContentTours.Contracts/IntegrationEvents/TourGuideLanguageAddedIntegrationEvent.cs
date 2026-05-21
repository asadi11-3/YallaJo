using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideLanguageAddedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid LanguageId,
    string Proficiency
) : IntegrationEventBase;
