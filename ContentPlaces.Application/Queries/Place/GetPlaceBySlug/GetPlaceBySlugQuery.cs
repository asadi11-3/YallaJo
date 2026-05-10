using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceBySlug;

public sealed record GetPlaceBySlugQuery(string Slug) : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.PlaceBySlug(Slug);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    // CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001: per-slug tag is added so that a slug
    // rename can evict the stale cache entry without a broad sweep.  The broad
    // TagPlaces tag is preserved for parity with the existing list/detail queries.
    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagPlaces,
        ContentPlacesCacheKeys.TagForPlaceSlug(Slug),
    ];
}
