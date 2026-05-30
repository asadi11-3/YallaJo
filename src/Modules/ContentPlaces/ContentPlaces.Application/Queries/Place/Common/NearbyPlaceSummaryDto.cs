namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record NearbyPlaceSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Latitude,
    decimal Longitude,
    decimal AverageRating,
    double DistanceKm);
