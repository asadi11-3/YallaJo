using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourRejectedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid RejectedByUserId,
    string Reason,
    DateTime RejectedAt) : IntegrationEventBase;
