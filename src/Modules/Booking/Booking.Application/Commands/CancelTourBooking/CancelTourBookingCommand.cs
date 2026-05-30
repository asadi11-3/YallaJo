using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CancelTourBooking;

/// <summary>
/// Cancel a tour booking. The source (User / Provider / Admin) is determined inside the handler
/// based on the caller's identity, ownership relationships, and admin permission claim.
/// Provider- and admin-initiated cancels are always 100% refund; user-initiated cancels walk
/// the booking's snapshotted RefundPolicy tiers.
/// </summary>
public sealed record CancelTourBookingCommand(Guid BookingId, string? Reason)
    : ICommand<CancelTourBookingResult>;

public sealed record CancelTourBookingResult(
    Guid BookingId,
    BookingStatus Status,
    DateTime CancelledAt,
    CancellationSource Source,
    string? Reason,
    decimal RefundAmount,
    string Currency);
