using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetMyBookings;

/// <summary>
/// Lightweight list-item DTO for tour bookings.
/// Used by GET /api/v1/booking/my-bookings and GET /api/v1/booking/admin/all.
/// </summary>
public sealed record MyBookingItemDto(
    Guid Id,
    string Reference,
    BookingStatus Status,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    decimal TotalAmount,
    string Currency,
    bool IsInstantBooking,
    DateTime PaymentExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
