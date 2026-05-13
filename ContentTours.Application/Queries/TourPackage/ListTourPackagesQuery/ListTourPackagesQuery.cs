using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPackage.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentTours.Application.Queries.TourPackage.ListTourPackages;

public sealed record ListTourPackagesQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ProviderId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Currency = null,
    Guid? IncludeTourId = null,
    DateTime? ValidOnDate = null,
    string Sort = "newest")
    : IQuery<PaginatedResult<TourPackageSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentToursCacheKeys.PackagesList(
        page:             Math.Max(1, Page),
        pageSize:         Math.Clamp(PageSize, 1, 50),
        providerId:       ProviderId,
        minPrice:         MinPrice,
        maxPrice:         MaxPrice,
        currency:         string.IsNullOrWhiteSpace(Currency) ? null : Currency.Trim().ToUpperInvariant(),
        includeTourId:    IncludeTourId,
        effectiveDateUtc: (ValidOnDate ?? DateTime.UtcNow).Date,
        sort:             string.IsNullOrWhiteSpace(Sort) ? "newest" : Sort.Trim().ToLowerInvariant());

    public TimeSpan? CacheDuration => null;

    public IReadOnlyList<string> Tags =>
        new[]
        {
            ContentToursCacheKeys.TagPackages,
            ContentToursCacheKeys.TagPackagesList,
        };
}
