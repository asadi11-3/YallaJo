using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.ListPlaceBusinesses
{
    public sealed class ListPlaceBusinessesQueryHandler(
     IBusinessRepository businessRepository)
     : IQueryHandler<ListPlaceBusinessesQuery, PaginatedResult<BusinessSummaryDto>>
    {
        public async Task<Result<PaginatedResult<BusinessSummaryDto>>> Handle(
            ListPlaceBusinessesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var paginatedResult = await businessRepository.SelectPaginatedAsync(
                    pageNumber: request.Page,
                    pageSize: request.PageSize,
                    selector: b => new BusinessSummaryDto(
                        b.Id,
                        b.Name,
                        b.Slug,
                        b.BusinessType.ToString(),
                        b.Status.ToString(),
                        b.AverageRating,
                        b.ReviewCount,
                        b.CreatedAt),
                    filter: b => b.PlaceId == request.PlaceId &&
                                 (b.Status == BusinessStatus.Approved ||
                                  request.IsAdmin ||
                                  (request.UserId.HasValue && b.OwnerId == request.UserId.Value)),
                    orderBy: q => q.OrderByDescending(b => b.CreatedAt),
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
}
