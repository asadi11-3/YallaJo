using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.ConfirmTourBooking;

/// <summary>
/// Provider (or admin) confirms a booking that is in PendingConfirmation (non-instant) or AwaitingPayment (instant
/// bookings auto-confirmed by payment webhook normally hit AwaitingPayment too).
/// </summary>
public sealed record ConfirmTourBookingCommand(Guid BookingId) : ICommand<ConfirmTourBookingResult>;
