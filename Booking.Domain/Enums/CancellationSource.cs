namespace Booking.Domain.Enums;

/// <summary>
/// Identifies who initiated a booking cancellation. Drives refund-percentage policy.
/// </summary>
public enum CancellationSource : byte
{
    /// <summary>End-user cancelled their own booking; refund per RefundPolicy tiers.</summary>
    User = 0,
    /// <summary>Tour provider cancelled the booking; always 100% refund.</summary>
    Provider = 1,
    /// <summary>Platform admin cancelled (force-majeure override); always 100% refund.</summary>
    Admin = 2,
    /// <summary>Cancelled by background service (payment expired, etc.).</summary>
    System = 3
}
