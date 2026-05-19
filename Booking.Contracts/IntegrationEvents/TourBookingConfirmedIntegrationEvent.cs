using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published when a booking transitions to Confirmed (instant booking on payment, provider confirm, or auto-accept).
/// Logical name <c>booking.tour-booking.confirmed.v1</c>.
/// </summary>
/// <param name="ConfirmationSource">String identifier: <c>PaymentWebhook</c>, <c>Manual</c>, or <c>AutoAccept</c>.</param>
public sealed record TourBookingConfirmedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    DateTime ConfirmedAt,
    string ConfirmationSource) : IntegrationEventBase;
