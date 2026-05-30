using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record GuideApplicationApprovedIntegrationEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid TourId,
    Guid ApprovedByUserId) : IntegrationEventBase;
