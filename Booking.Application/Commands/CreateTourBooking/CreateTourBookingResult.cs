using Booking.Domain.Enums;

namespace Booking.Application.Commands.CreateTourBooking;

/// <summary>
/// Result of a successful <see cref="CreateTourBookingCommand"/>.
/// Returned alongside <see cref="YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome.Created"/>.
/// </summary>
public sealed record CreateTourBookingResult(
    Guid Id,
    string Reference,
    BookingStatus Status,
    Guid TourId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    BookingPricingDto Pricing,
    DateTime PaymentExpiresAt,
    string PaymentToken);

/// <summary>
/// Pricing snapshot exposed to the caller. Commission rate/amount intentionally omitted
/// (kept on the aggregate for payout calculation only).
/// </summary>
public sealed record BookingPricingDto(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal LoyaltyAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<BookingLineItemDto> LineItems);

public sealed record BookingLineItemDto(TierType TierType, int Count, decimal UnitPrice);
