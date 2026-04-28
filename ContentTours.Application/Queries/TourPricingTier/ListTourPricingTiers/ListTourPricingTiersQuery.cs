using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPricingTier.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;

public sealed record ListTourPricingTiersQuery(
    Guid TourId,
    bool ActiveOnly,
    string? LanguageCode)
    : IQuery<IReadOnlyList<TourPricingTierDto>>, ICacheableQuery
{
    public string CacheKey => TourPricingTierCacheKeys.List(TourId, ActiveOnly, LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourPricingTierCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
