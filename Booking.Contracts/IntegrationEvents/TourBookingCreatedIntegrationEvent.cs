using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record TourBookingCreatedIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId,
    int ParticipantCount,
    DateTime BookedAt) : IntegrationEventBase;
