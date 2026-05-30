namespace Booking.Domain.ValueObjects;

/// <summary>
/// Result of the booking pricing calculation. Constructed by the
/// CreateTourBookingCommandHandler in Step 3 and passed to <c>TourBooking.Create</c>.
/// </summary>
/// <param name="Subtotal">Sum of all line items before discounts/loyalty.</param>
/// <param name="DiscountAmount">Promo/code discount applied (>= 0).</param>
/// <param name="LoyaltyAmount">Loyalty-points redemption (>= 0).</param>
/// <param name="TotalAmount">Final payable amount = Subtotal - DiscountAmount - LoyaltyAmount.</param>
/// <param name="CommissionRate">Platform commission rate (0..1) applied to <paramref name="TotalAmount"/>.</param>
/// <param name="CommissionAmount">Computed commission = Round(TotalAmount * CommissionRate, 2).</param>
/// <param name="Currency">ISO 4217 currency code (must be uniform across line items).</param>
/// <param name="LineItems">Per-tier breakdown serialised to LineItemsJson column.</param>
public sealed record BookingPricing(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal LoyaltyAmount,
    decimal TotalAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    string Currency,
    IReadOnlyList<BookingLineItem> LineItems);
