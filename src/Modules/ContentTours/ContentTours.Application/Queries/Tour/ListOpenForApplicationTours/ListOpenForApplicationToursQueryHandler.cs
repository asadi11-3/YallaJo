using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.ListOpenForApplicationTours;

public sealed class ListOpenForApplicationToursQueryHandler(
    ITourRepository tourRepository,
    ILogger<ListOpenForApplicationToursQueryHandler> logger)
    : IQueryHandler<ListOpenForApplicationToursQuery, ListOpenForApplicationToursResult>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListOpenForApplicationToursResult>> Handle(
        ListOpenForApplicationToursQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var q = request.Q?.Trim() ?? string.Empty;

            // City intentionally null: Tour only carries PlaceId — Place lives in the
            // ContentPlaces module and is not navigable/projectable from this repository.
            var paged = await tourRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize:   pageSize,
                    selector:   t => new OpenForApplicationTourDto(
                        t.Id,
                        t.Name,
                        t.Slug,
                        null,
                        t.BasePrice.Amount,
                        t.Currency),
                    filter: t => t.Status == TourStatus.Approved
                              && t.IsOpenForApplications
                              && (string.IsNullOrEmpty(q) || t.Name.Contains(q)),
                    orderBy: src => src.OrderBy(t => t.Name).ThenBy(t => t.Id),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "ListOpenForApplicationTours q='{Q}' page={Page} size={PageSize}: {Count}/{Total}",
                q, page, pageSize, paged.Items.Count, paged.TotalCount);

            return Result.Success(new ListOpenForApplicationToursResult(paged.Items, paged.TotalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<ListOpenForApplicationToursResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
