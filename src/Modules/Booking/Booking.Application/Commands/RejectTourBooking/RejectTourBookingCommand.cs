using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.RejectTourBooking;

/// <summary>
/// Provider rejects a booking that is in PendingConfirmation state.
/// Reject ALWAYS triggers automatic 100% refund regardless of policy.
/// </summary>
public sealed record RejectTourBookingCommand(Guid BookingId, string Reason) : ICommand<RejectTourBookingResult>;
