using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideSuspendedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    string Reason,
    Guid SuspendedByAdminId,
    DateTime SuspendedAt) : IntegrationEventBase;
