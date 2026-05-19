using Booking.Domain.Enums;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Per-tier price breakdown line. Stored as part of the LineItemsJson column on TourBooking.
/// </summary>
/// <param name="TierType">Participant tier (Adult/Child/Infant/Senior/Group/Private).</param>
/// <param name="Count">Number of participants in this tier.</param>
/// <param name="UnitPrice">Per-participant price in the booking's currency.</param>
/// <param name="Currency">ISO 4217 currency code (matches the parent booking).</param>
public sealed record BookingLineItem(
    TierType TierType,
    int Count,
    decimal UnitPrice,
    string Currency)
{
    /// <summary>Convenience: total amount contributed by this line.</summary>
    public decimal LineTotal => UnitPrice * Count;
}
