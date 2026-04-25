using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceById;

public sealed record GetPlaceByIdQuery(Guid PlaceId) : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.Place(PlaceId);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => ["places", $"place:{PlaceId}"];
}
