using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.GetById;

public sealed record GetTourGuideByIdQuery(Guid TourGuideId)
    : IQuery<TourGuideProfileDto>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.Profile(TourGuideId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForProfile(TourGuideId)];
}
