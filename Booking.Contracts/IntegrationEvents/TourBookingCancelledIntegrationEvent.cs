using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record TourBookingCancelledIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId,
    string Reason) : IntegrationEventBase;
