using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record GuideApplicationRejectedIntegrationEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid TourId,
    string Reason) : IntegrationEventBase;
