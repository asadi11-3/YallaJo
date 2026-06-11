namespace YallaJo.Web.Areas.Business.Models.Amenities;

public static class AmenitiesMapper
{
    public static IReadOnlyList<AmenityRowVm> ToRows(IReadOnlyList<BusinessAmenityItemResponse> items) =>
        items
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new AmenityRowVm(a.Id, a.Name, a.Icon, a.SortOrder))
            .ToList();

    /// <summary>
    /// D-13: curated amenity icon choices (value = Font Awesome class, suffix keys into
    /// Business.Amenities.Icon.{Suffix} resx labels). Replaces the free-text icon input.
    /// </summary>
    public static IReadOnlyList<(string Value, string ResxSuffix)> IconOptions { get; } =
    [
        ("fa-wifi", "Wifi"),
        ("fa-square-parking", "Parking"),
        ("fa-utensils", "Restaurant"),
        ("fa-mug-saucer", "Coffee"),
        ("fa-snowflake", "AirConditioning"),
        ("fa-wheelchair", "Wheelchair"),
        ("fa-paw", "PetFriendly"),
        ("fa-person-swimming", "Pool"),
        ("fa-dumbbell", "Gym"),
        ("fa-spa", "Spa"),
        ("fa-bed", "Rooms"),
        ("fa-car", "Valet"),
        ("fa-music", "LiveMusic"),
        ("fa-tv", "Tv"),
        ("fa-baby", "FamilyFriendly"),
        ("fa-smoking", "SmokingArea"),
        ("fa-credit-card", "CardPayment"),
        ("fa-bolt", "EvCharging"),
        ("fa-shield-halved", "Security"),
        ("fa-leaf", "Garden")
    ];
}
