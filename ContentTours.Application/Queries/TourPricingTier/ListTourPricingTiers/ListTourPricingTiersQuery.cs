using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPricingTier.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;

public sealed record ListTourPricingTiersQuery(
    Guid TourId,
    bool ActiveOnly,
    string? LanguageCode,
    /// <summary>The authenticated user's id, or null for anonymous callers.</summary>
    Guid? CallerUserId,
    /// <summary>True when the caller carries the Admin role.</summary>
    bool IsAdmin)
    : IQuery<IReadOnlyList<TourPricingTierDto>>, ICacheableQuery
{
    public string CacheKey => TourPricingTierCacheKeys.List(
        TourId, ActiveOnly, LanguageCode, CallerUserId, IsAdmin);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourPricingTierCacheKeys.TagForTour(TourId), ContentToursCacheKeys.TagForTour(TourId)];
}
