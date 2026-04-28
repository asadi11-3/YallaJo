using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

public sealed record SuggestToursQuery(string Q, string LanguageCode = "en")
    : IQuery<IReadOnlyList<TourSuggestDto>>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.Suggest(Q, LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["tours:suggest", "tours:list"];
}
