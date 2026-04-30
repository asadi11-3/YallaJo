using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.GetTourById;

public sealed record GetTourByIdQuery(Guid Id, string? AcceptLanguage = null)
    : IQuery<TourDetailDto>, ICacheableQuery
{
    public string CacheKey =>
        ContentToursCacheKeys.Tour(Id, AcceptLanguage, isElevated: false);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
        [ContentToursCacheKeys.TagToursList, ContentToursCacheKeys.TagForTour(Id)];
}
