namespace ContentPlaces.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the ContentPlaces bounded context.
/// </summary>
public static class ContentPlacesFeatures
{
    public const string Place               = nameof(Place);
    public const string Business            = nameof(Business);
    public const string BusinessHours       = nameof(BusinessHours);
    public const string BusinessStaff       = nameof(BusinessStaff);
    public const string BusinessAmenity     = nameof(BusinessAmenity);
    public const string AccessibilityFeature = nameof(AccessibilityFeature);
    public const string ServiceItem         = nameof(ServiceItem);
    public const string Booking             = nameof(Booking);
}
