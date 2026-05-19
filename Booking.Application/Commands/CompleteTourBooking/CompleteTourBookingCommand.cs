using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CompleteTourBooking;

/// <summary>
/// Marks a Confirmed tour booking as Completed. Caller must be the provider of the
/// tour (or an admin) and the slot's start time must already be in the past.
/// </summary>
public sealed record CompleteTourBookingCommand(Guid BookingId) : ICommand<CompleteTourBookingResult>;

/// <summary>
/// Result envelope returned to the caller after a successful Complete operation.
/// </summary>
public sealed record CompleteTourBookingResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime CompletedAt,
    Guid CompletedByUserId);
