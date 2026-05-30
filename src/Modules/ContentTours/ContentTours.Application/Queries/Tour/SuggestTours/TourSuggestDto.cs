namespace ContentTours.Application.Queries.Tour.SuggestTours;

public sealed record TourSuggestDto(Guid Id, string Name, string Slug, string? ThumbnailUrl);
