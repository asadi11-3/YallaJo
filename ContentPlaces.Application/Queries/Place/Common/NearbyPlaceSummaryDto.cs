namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record NearbyPlaceSummaryDto(
    Guid Id,
    string Name,
    string? PrimaryImageUrl,
    double AverageRating,
    double DistanceKm,
    string Category);
