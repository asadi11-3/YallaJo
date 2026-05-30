using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Contracts.IntegrationEvents;

public sealed record LiveTrackingSessionStartedIntegrationEvent(
    Guid SessionId,
    Guid UserId,
    Guid TourBookingId,
    DateTime StartedAt) : IntegrationEventBase;
