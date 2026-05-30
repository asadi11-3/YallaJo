namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record MapPinDto(
    Guid Id,
    string Name,
    double Lat,
    double Lng,
    string? PrimaryImageUrl,
    double AverageRating,
    int TourCount);
