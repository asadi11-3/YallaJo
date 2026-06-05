namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record PlaceImageDto(
    string Url,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary);
