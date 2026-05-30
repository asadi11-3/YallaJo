using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourApprovedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt) : IntegrationEventBase;
