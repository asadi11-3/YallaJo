using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Application.Specifications;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.ListPlaces;

public sealed class ListPlacesQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<ListPlacesQueryHandler> logger)
    : IQueryHandler<ListPlacesQuery, PaginatedResult<PlaceSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<PlaceSummaryDto>>> Handle(
        ListPlacesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, MaxPageSize);

            var spec = new PlaceFilterSpecification(
                page:      request.Page,
                pageSize:  pageSize,
                ratingMin: request.RatingMin,
                ratingMax: request.RatingMax,
                city:      request.City,
                country:   request.Country);

            var paged = await placeRepository.PaginatedListAsync(spec, cancellationToken);

            var dtos = paged.Items
                .Select(PlaceSummaryDto.From)
                .ToList();

            return Result<PaginatedResult<PlaceSummaryDto>>.Success(
                new PaginatedResult<PlaceSummaryDto>(dtos, paged.TotalCount, request.Page, pageSize));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<PlaceSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
