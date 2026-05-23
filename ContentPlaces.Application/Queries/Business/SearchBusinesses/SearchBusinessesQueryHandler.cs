using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.SearchBusinesses;

public sealed class SearchBusinessesQueryHandler(
    IBusinessRepository businessRepository,
    ILogger<SearchBusinessesQueryHandler> logger)
    : IQueryHandler<SearchBusinessesQuery, PaginatedResult<BusinessSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<BusinessSummaryDto>>> Handle(
        SearchBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, MaxPageSize);

            // Parse optional BusinessType filter.
            BusinessType? businessTypeFilter = null;
            if (!string.IsNullOrWhiteSpace(request.BusinessType) &&
                Enum.TryParse<BusinessType>(request.BusinessType, ignoreCase: true, out var parsed))
            {
                businessTypeFilter = parsed;
            }

            var queryText = request.Query?.Trim().ToLowerInvariant();

            var paginatedResult = await businessRepository.SelectPaginatedAsync(
                pageNumber: request.Page,
                pageSize: pageSize,
                selector: b => new BusinessSummaryDto(
                    b.Id,
                    b.Name,
                    b.Slug,
                    b.BusinessType.ToString(),
                    b.Status.ToString(),
                    (double)b.Location.Latitude,
                    (double)b.Location.Longitude,
                    b.City,
                    b.Country,
                    b.AverageRating,
                    b.ReviewCount,
                    b.IsVerified,
                    b.IsFeatured,
                    null),
                filter: b => b.Status == BusinessStatus.Approved
                    && (queryText == null || b.Name.ToLower().Contains(queryText))
                    && (!businessTypeFilter.HasValue || b.BusinessType == businessTypeFilter.Value)
                    && (request.City == null || b.City == request.City)
                    && (request.Country == null || b.Country == request.Country),
                orderBy: q => q.OrderByDescending(b => b.IsFeatured)
                               .ThenByDescending(b => b.AverageRating)
                               .ThenBy(b => b.Name),
                ct: cancellationToken);

            return Result<PaginatedResult<BusinessSummaryDto>>.Success(paginatedResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<BusinessSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
