namespace Booking.Domain.Enums;

/// <summary>
/// Status of a provider as seen by the Booking module's snapshot.
/// </summary>
public enum BookingProviderStatus
{
    Active = 0,
    Suspended = 1,
    PendingApproval = 2,
    Disabled = 3,
}
