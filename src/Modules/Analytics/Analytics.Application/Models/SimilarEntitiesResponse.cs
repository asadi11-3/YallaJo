namespace Analytics.Application.Models;

public sealed record SimilarEntitiesResponse(IReadOnlyList<RecommendationItemDto> Items);
