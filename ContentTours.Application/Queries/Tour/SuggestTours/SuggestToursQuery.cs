using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.SuggestTours;

/// <summary>
/// Autocomplete query for the tour search box. Matches both <c>Tour.Name</c> and
/// <c>TourTranslation.Name</c> for the requested Accept-Language; the cache key is
/// partitioned by normalized language so an Arabic suggest call cannot poison the
/// English bucket (and vice-versa).
/// </summary>
/// <param name="Q">Prefix to match (≥ 2 chars per validator).</param>
/// <param name="AcceptLanguage">
/// Either a single language code (<c>"ar"</c>, <c>"en-US"</c>) or a full
/// <c>Accept-Language</c> header value. The handler resolves it to a language id
/// via <c>AcceptLanguageResolver</c>; the cache-key helper normalises it.
/// </param>
public sealed record SuggestToursQuery(string Q, string? AcceptLanguage = null)
    : IQuery<IReadOnlyList<TourSuggestDto>>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.Suggest(Q, AcceptLanguage);
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["tours:suggest", "tours:list"];
}
