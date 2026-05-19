using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetTourBookingById;

/// <summary>
/// Full booking projection returned by <c>GET /api/v1/booking/{id}</c>.
/// Mirrors the create-response shape but omits the payment-initiation token
/// and adds optional cancellation / rejection / completion blocks that appear
/// once the booking has progressed past <see cref="BookingStatus.AwaitingPayment"/>.
/// </summary>
public sealed record TourBookingDetailDto(
    Guid Id,
    string Reference,
    BookingStatus Status,
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    TourBookingPricingDto Pricing,
    bool IsInstantBooking,
    DateTime PaymentExpiresAt,
    string? SpecialRequests,
    TourBookingConfirmationDto? Confirmation,
    TourBookingRejectionDto? Rejection,
    TourBookingCancellationDto? Cancellation,
    TourBookingCompletionDto? Completion,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record TourBookingPricingDto(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal LoyaltyAmount,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<TourBookingLineItemDto> LineItems);

public sealed record TourBookingLineItemDto(TierType TierType, int Count, decimal UnitPrice);

public sealed record TourBookingConfirmationDto(DateTime ConfirmedAt, ConfirmationSource Source);

public sealed record TourBookingRejectionDto(DateTime RejectedAt, string Reason);

public sealed record TourBookingCancellationDto(
    DateTime CancelledAt,
    CancellationSource Source,
    string? Reason,
    decimal? RefundAmount);

public sealed record TourBookingCompletionDto(DateTime CompletedAt, Guid? CompletedByUserId);
