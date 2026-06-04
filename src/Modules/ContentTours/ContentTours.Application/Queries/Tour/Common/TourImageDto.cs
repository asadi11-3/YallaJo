namespace ContentTours.Application.Queries.Tour.Common;

public sealed record TourImageDto(
    string Url,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary);
