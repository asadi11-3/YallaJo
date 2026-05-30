using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetAllBookings;

/// <summary>
/// Admin-scoped booking list item. Adds <see cref="UserId"/> and a few extra audit fields
/// over <c>MyBookingItemDto</c> so the operator dashboard can show ownership + lifecycle at a glance.
/// </summary>
public sealed record AdminBookingItemDto(
    Guid Id,
    string Reference,
    BookingStatus Status,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    decimal TotalAmount,
    string Currency,
    bool IsInstantBooking,
    DateTime PaymentExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? RejectedAt,
    DateTime? CancelledAt,
    DateTime? CompletedAt,
    decimal? RefundAmount,
    DateTime CreatedAt);
