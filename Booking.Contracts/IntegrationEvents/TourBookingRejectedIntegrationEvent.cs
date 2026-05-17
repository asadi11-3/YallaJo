using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record TourBookingRejectedIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    string Reason) : IntegrationEventBase;
