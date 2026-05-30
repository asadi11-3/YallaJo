namespace Analytics.Application.Models;

public sealed record RecommendationsResponse(
    IReadOnlyList<RecommendationItemDto> Items,
    bool IsPersonalized,
    DateTime? ComputedAt);
