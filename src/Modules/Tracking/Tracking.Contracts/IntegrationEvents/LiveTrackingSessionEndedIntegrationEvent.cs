using YallaJo.SharedKernel.Domain.Event;

namespace Tracking.Contracts.IntegrationEvents;

public sealed record LiveTrackingSessionEndedIntegrationEvent(
    Guid SessionId,
    Guid UserId,
    DateTime EndedAt,
    string Reason) : IntegrationEventBase;
