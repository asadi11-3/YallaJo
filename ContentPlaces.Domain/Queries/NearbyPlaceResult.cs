namespace ContentPlaces.Domain.Queries;

/// <summary>
/// Flat projection returned by the Haversine SQL query in <see cref="IPlaceRepository.GetNearbyAsync"/>.
/// Kept in the Domain layer so Application handlers can reference it without depending on Infrastructure.
/// </summary>
public sealed record NearbyPlaceResult(
    Guid Id,
    string Name,
    string Slug,
    decimal Latitude,
    decimal Longitude,
    decimal AverageRating,
    double DistanceKm);

public sealed record NearbyBusinessResult(
    Guid Id,
    string Name,
    string Slug,
    int BusinessType,
    int Status,
    decimal Latitude,
    decimal Longitude,
    string? City,
    string? Country,
    decimal AverageRating,
    int ReviewCount,
    bool IsVerified,
    bool IsFeatured,
    double DistanceKm);
