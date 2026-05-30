using System.Globalization;
using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.SearchTours;

/// <summary>Request parameters for the search endpoint (also used as AppliedFilters in response).</summary>
public sealed record SearchToursRequest(
    string? Q,
    Guid? PlaceId,
    decimal? PriceMin,
    decimal? PriceMax,
    string? Difficulty,
    int? DurationMinutesMin,
    int? DurationMinutesMax,
    bool? IsChildFriendly,
    bool? IsAccessible,
    bool? IsInstantBooking,
    bool? HasDiscount,
    decimal? MinRating,
    string? LanguageCode,
    SearchSort Sort = SearchSort.Relevance,
    int Page = 1,
    int PageSize = 20);

public sealed record SearchToursQuery(SearchToursRequest Request)
    : IQuery<SearchToursResult>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.Search(BuildCanonicalKey(Request));
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => [ContentToursCacheKeys.TagToursSearch, ContentToursCacheKeys.TagToursList];

    /// <summary>
    /// Builds a deterministic, sort-stable canonical string covering every
    /// request field that affects the result. Replaces the prior
    /// <c>JsonSerializer.Serialize + SHA1</c> approach so the key cannot drift
    /// across runtimes/serializer settings, and so that a future field added
    /// to <see cref="SearchToursRequest"/> requires an explicit edit here
    /// instead of silently changing every cache key.
    /// </summary>
    private static string BuildCanonicalKey(SearchToursRequest r)
    {
        var inv = CultureInfo.InvariantCulture;
        var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["q"]                  = (r.Q ?? string.Empty).Trim().ToLowerInvariant(),
            ["placeId"]            = r.PlaceId?.ToString() ?? string.Empty,
            ["priceMin"]           = r.PriceMin?.ToString(inv) ?? string.Empty,
            ["priceMax"]           = r.PriceMax?.ToString(inv) ?? string.Empty,
            ["difficulty"]         = (r.Difficulty ?? string.Empty).ToLowerInvariant(),
            ["durationMinutesMin"] = r.DurationMinutesMin?.ToString(inv) ?? string.Empty,
            ["durationMinutesMax"] = r.DurationMinutesMax?.ToString(inv) ?? string.Empty,
            ["isChildFriendly"]    = r.IsChildFriendly?.ToString() ?? string.Empty,
            ["isAccessible"]       = r.IsAccessible?.ToString() ?? string.Empty,
            ["isInstantBooking"]   = r.IsInstantBooking?.ToString() ?? string.Empty,
            ["hasDiscount"]        = r.HasDiscount?.ToString() ?? string.Empty,
            ["minRating"]          = r.MinRating?.ToString(inv) ?? string.Empty,
            ["lang"]               = ContentToursCacheKeys.NormalizeLanguage(r.LanguageCode),
            ["sort"]               = r.Sort.ToString(),
            ["page"]               = r.Page.ToString(inv),
            ["pageSize"]           = r.PageSize.ToString(inv),
        };

        return string.Join("&", pairs.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
