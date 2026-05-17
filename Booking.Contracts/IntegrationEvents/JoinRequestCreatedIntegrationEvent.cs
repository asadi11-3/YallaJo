using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record JoinRequestCreatedIntegrationEvent(
    Guid JoinRequestId,
    Guid TourBookingId,
    Guid UserId) : IntegrationEventBase;
