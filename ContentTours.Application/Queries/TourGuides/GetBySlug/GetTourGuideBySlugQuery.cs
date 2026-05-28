using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.GetBySlug;

public sealed record GetTourGuideBySlugQuery(string Slug)
    : IQuery<TourGuideProfileDto>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.ProfileBySlug(Slug);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForProfileSlug(Slug)];
}
