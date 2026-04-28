using ContentTours.Application.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public string CacheKey => TourSearchCacheKeys.Search(ComputeHash(Request));
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => ["tours:search", "tours:list"];

    private static string ComputeHash(SearchToursRequest req)
    {
        var json = JsonSerializer.Serialize(req);
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
