namespace Booking.Domain.Enums;

/// <summary>
/// Participant pricing tier on a tour booking. Determines per-head price.
/// </summary>
public enum TierType : byte
{
    Adult = 0,
    Child = 1,
    Infant = 2,
    Senior = 3,
    Group = 4,
    Private = 5
}
