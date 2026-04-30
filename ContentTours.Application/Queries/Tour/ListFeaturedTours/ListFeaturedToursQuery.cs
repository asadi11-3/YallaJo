using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.ListFeaturedTours;

public sealed record ListFeaturedToursQuery(string LanguageCode = "en")
    : IQuery<IReadOnlyList<TourSummaryDto>>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.Featured(LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => ["tours:featured", "tours:list"];
}
