using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetSeasonalityRules;

public sealed record GetSeasonalityRulesQuery : IQuery<GetSeasonalityRulesResult>, ICacheableQuery
{
    public string CacheKey => "ct:analytics:admin:seasonality";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => ["analytics:seasonality"];
}

public sealed record GetSeasonalityRulesResult(IReadOnlyList<SeasonalityRuleDto> Rules);

public sealed record SeasonalityRuleDto(
    Guid Id,
    Guid PlaceId,
    int MonthStart,
    int MonthEnd,
    decimal Multiplier,
    string? Description,
    bool IsActive);
