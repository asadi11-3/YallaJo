namespace Booking.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the Booking bounded context.
/// </summary>
public static class BookingFeatures
{
    public const string TourBooking        = nameof(TourBooking);
    public const string AvailabilitySlot   = nameof(AvailabilitySlot);
    public const string RefundPolicy       = nameof(RefundPolicy);
    public const string JoinRequest        = nameof(JoinRequest);
    public const string ProviderDocument   = nameof(ProviderDocument);
    public const string SlotLock           = nameof(SlotLock);
    public const string BookingAdmin       = nameof(BookingAdmin);
    public const string BookingReports     = nameof(BookingReports);
}
