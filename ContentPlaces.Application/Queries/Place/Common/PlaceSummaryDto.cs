using ContentPlaces.Domain.Enums;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record PlaceSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    PlaceType PlaceType,
    decimal Latitude,
    decimal Longitude,
    string? City,
    string? Country,
    decimal AverageRating,
    int ReviewCount,
    bool IsFeatured,
    bool IsVerified)
{
    public static PlaceSummaryDto From(PlaceEntity place) => new(
        Id:            place.Id,
        Name:          place.Name,
        Slug:          place.Slug,
        PlaceType:     place.PlaceType,
        Latitude:      place.Location.Latitude,
        Longitude:     place.Location.Longitude,
        City:          place.City,
        Country:       place.Country,
        AverageRating: place.AverageRating,
        ReviewCount:   place.ReviewCount,
        IsFeatured:    place.IsFeatured,
        IsVerified:    place.IsVerified);
}

