using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record GuideTourOfferingSuspendedIntegrationEvent(
    Guid OfferingId,
    Guid TourId,
    Guid TourGuideId,
    Guid GuideUserId,
    string Reason,
    Guid SuspendedByAdminId,
    DateTime SuspendedAt) : IntegrationEventBase;
