using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Tests.Unit;

internal static class TestPlaceFactory
{
    public static Place CreatePlace(
        Guid createdByUserId,
        string name = "Test Place",
        string slug = "test-place",
        decimal latitude = 31.95m,
        decimal longitude = 35.93m,
        PlaceType placeType = PlaceType.Attraction)
    {
        return Place.Create(
            name: name,
            slug: slug,
            placeType: placeType,
            latitude: latitude,
            longitude: longitude,
            createdByUserId: createdByUserId);
    }
}
