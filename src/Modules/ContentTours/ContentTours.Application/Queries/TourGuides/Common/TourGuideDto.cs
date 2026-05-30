namespace ContentTours.Application.Queries.TourGuides.Common;

public sealed record TourGuideDto(
    Guid TourGuideId,
    bool IsPrimary,
    string DisplayName,
    string? AvatarUrl);
