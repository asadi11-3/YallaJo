using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using TourEntity = ContentTours.Domain.Entities.Tour;

namespace ContentTours.Application.Queries.Tour.ListAdminTours;

public sealed class ListAdminToursQueryHandler(
    ITourRepository tourRepository,
    ILogger<ListAdminToursQueryHandler> logger)
    : IQueryHandler<ListAdminToursQuery, PaginatedResult<AdminTourSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<AdminTourSummaryDto>>> Handle(
        ListAdminToursQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            TourStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!Enum.TryParse<TourStatus>(request.Status, ignoreCase: true, out var parsed))
                {
                    return Result.Invalid<PaginatedResult<AdminTourSummaryDto>>(
                        new Error(
                            "Tour.InvalidStatusFilter",
                            $"'{request.Status}' is not a valid status. " +
                            $"Valid values: {string.Join(", ", Enum.GetNames<TourStatus>())}."));
                }

                statusFilter = parsed;
            }

            var paged = await tourRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize:   pageSize,
                    selector:   t => new AdminTourSummaryDto(
                        t.Id,
                        t.Name,
                        t.Slug,
                        t.Status.ToString(),
                        t.BasePrice.Amount,
                        t.Currency,
                        t.CreatedAt,
                        t.SubmittedAt,
                        t.CreatedByUserId),
                    filter: t => !t.IsDeleted
                              && (statusFilter == null || t.Status == statusFilter.Value),
                    orderBy: q => OrderBy(q, request.Sort),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            return Result<PaginatedResult<AdminTourSummaryDto>>.Success(paged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<AdminTourSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static IOrderedQueryable<TourEntity> OrderBy(IQueryable<TourEntity> query, string? sort) =>
        sort?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(t => t.SubmittedAt ?? t.CreatedAt),
            _        => query.OrderByDescending(t => t.SubmittedAt ?? t.CreatedAt),
        };
}
