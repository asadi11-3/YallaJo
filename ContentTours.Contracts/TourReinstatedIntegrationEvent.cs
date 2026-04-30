using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourReinstatedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime ReinstatedAt) : IntegrationEventBase;
