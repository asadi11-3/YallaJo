using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourSuspendedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    string Reason,
    DateTime SuspendedAt) : IntegrationEventBase;
