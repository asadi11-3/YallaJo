using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPackage.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourPackage.GetTourPackageById;

public sealed record GetTourPackageByIdQuery(Guid Id, string? AcceptLanguage = null)
    : IQuery<TourPackageDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentToursCacheKeys.Package(Id, AcceptLanguage);

    public TimeSpan? CacheDuration => null;

    public IReadOnlyList<string> Tags =>
        new[]
        {
            ContentToursCacheKeys.TagPackages,
            ContentToursCacheKeys.TagForPackage(Id),
        };
}
