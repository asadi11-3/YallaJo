using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record TourBookingCompletedIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId) : IntegrationEventBase;
