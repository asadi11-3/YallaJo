using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourPackage.ListTourPackages;

public sealed class ListTourPackagesQueryHandler(
    ITourPackageRepository repository,
    ILogger<ListTourPackagesQueryHandler> logger)
    : IQueryHandler<ListTourPackagesQuery, PaginatedResult<TourPackageSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<TourPackageSummaryDto>>> Handle(
        ListTourPackagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var effectiveDate = (request.ValidOnDate ?? DateTime.UtcNow).Date;
            var sort = ParseSort(request.Sort);

            var (rows, total) = await repository
                .GetPagedSummariesAsync(
                    page:             page,
                    pageSize:         pageSize,
                    providerId:       request.ProviderId,
                    minPrice:         request.MinPrice,
                    maxPrice:         request.MaxPrice,
                    currency:         request.Currency,
                    includeTourId:    request.IncludeTourId,
                    effectiveDateUtc: effectiveDate,
                    sort:             sort,
                    ct:               cancellationToken)
                .ConfigureAwait(false);

            var dtos = rows
                .Select(r => new TourPackageSummaryDto(
                    Id:                 r.Id,
                    Name:               r.Name,
                    Description:        r.Description,
                    PriceAmount:        r.PriceAmount,
                    Currency:           r.Currency,
                    MaxParticipants:    r.MaxParticipants,
                    ValidFrom:          r.ValidFrom,
                    ValidTo:            r.ValidTo,
                    IncludedTourCount:  r.IncludedTourCount,
                    CreatedAt:          r.CreatedAt,
                    CoverImageUrl:      r.CoverImageUrl))
                .ToList();

            var result = new PaginatedResult<TourPackageSummaryDto>(dtos, total, page, pageSize);

            logger.LogDebug(
                "Listed TourPackages: page={Page}, pageSize={PageSize}, total={Total}",
                page, pageSize, total);

            return Result.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<PaginatedResult<TourPackageSummaryDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static TourPackageSortOption ParseSort(string? sort) =>
        (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "price_asc"            => TourPackageSortOption.PriceAscending,
            "price_desc"           => TourPackageSortOption.PriceDescending,
            "validity_ending_soon" => TourPackageSortOption.ValidityEndingSoon,
            _                      => TourPackageSortOption.Newest,
        };
}
