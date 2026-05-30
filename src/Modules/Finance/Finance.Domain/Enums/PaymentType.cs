namespace Finance.Domain.Enums;

/// <summary>
/// Distinguishes forward charges from reversals.
/// </summary>
/// <remarks>
/// Refund rows live in the same Payment table as their originating Booking row.
/// They carry <c>PaymentType.Refund</c>, a negative or matching positive Amount,
/// and an <c>OriginalPaymentId</c> back-reference to the Booking row being reversed.
/// </remarks>
public enum PaymentType : byte
{
    /// <summary>
    /// Forward charge initiated by a buyer for a booking.
    /// </summary>
    Booking = 0,

    /// <summary>
    /// Reversal of a Booking-type payment. Always linked via OriginalPaymentId.
    /// </summary>
    Refund = 1,
}
