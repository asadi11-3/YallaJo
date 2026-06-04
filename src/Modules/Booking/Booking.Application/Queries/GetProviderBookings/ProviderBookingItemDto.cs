using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetProviderBookings;

/// <summary>
/// Provider-scoped booking list item. Carries the booking summary plus the slot
/// date/time (joined server-side to avoid Web N+1 hydration). Exposes only the
/// traveler's <see cref="UserId"/> — no traveler name/email (privacy: deferred).
/// </summary>
public sealed record ProviderBookingItemDto(
    Guid Id,
    string Reference,
    BookingStatus Status,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    DateOnly? SlotDate,
    TimeOnly? SlotStartTime,
    TimeOnly? SlotEndTime,
    int ParticipantCount,
    decimal TotalAmount,
    string Currency,
    bool IsInstantBooking,
    DateTime PaymentExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
