using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.ChildrenInfo.GetTourChildrenInfo;

public sealed record GetTourChildrenInfoQuery(Guid TourId)
    : IQuery<ChildrenInfoDto>, ICacheableQuery
{
    public string CacheKey => TourChildrenInfoCacheKeys.Get(TourId);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

    public IReadOnlyList<string> Tags =>
        [TourChildrenInfoCacheKeys.TagForTour(TourId), ContentToursCacheKeys.TagForTour(TourId)];
}
