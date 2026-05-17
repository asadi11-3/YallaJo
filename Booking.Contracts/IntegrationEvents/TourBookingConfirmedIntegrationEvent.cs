using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record TourBookingConfirmedIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId) : IntegrationEventBase;
