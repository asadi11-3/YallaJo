using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetRecommendations;

public sealed record GetRecommendationsQuery(
    Guid UserId,
    string? Language,
    int Limit = 20,
    bool HalalOnly = false,
    string? AcceptLanguage = null,
    bool ShowAllPrices = false) : IQuery<RecommendationsResponse>, ICacheableQuery
{
    public string CacheKey => $"ct:analytics:recs:user:{UserId:N}:lang:{Language ?? "en"}:limit:{Math.Clamp(Limit, 1, 100)}:halal:{HalalOnly}:allPrices:{ShowAllPrices}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [$"analytics:recs:{UserId:N}"];
}
