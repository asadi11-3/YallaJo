using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourSubmittedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime SubmittedAt) : IntegrationEventBase;
