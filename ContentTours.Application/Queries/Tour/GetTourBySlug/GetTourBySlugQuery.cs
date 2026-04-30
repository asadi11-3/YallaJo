using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.GetTourBySlug;

public sealed record GetTourBySlugQuery(string Slug, string? AcceptLanguage = null)
    : IQuery<TourDetailDto>, ICacheableQuery
{
    public string CacheKey =>
        ContentToursCacheKeys.TourBySlug(Slug, AcceptLanguage, isElevated: false);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [ContentToursCacheKeys.TagToursList];
}
