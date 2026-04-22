using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceBySlug;

public sealed record GetPlaceBySlugQuery(string Slug) : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.PlaceBySlug(Slug);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => ["places"];
}
