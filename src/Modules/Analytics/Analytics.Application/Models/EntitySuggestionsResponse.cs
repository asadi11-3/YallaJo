namespace Analytics.Application.Models;

public sealed record EntitySuggestionsResponse(IReadOnlyList<RecommendationItemDto> Items);
