using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.ListFeaturedTours;

public sealed class ListFeaturedToursQueryHandler(
    ITourRepository tourRepo,
    ILogger<ListFeaturedToursQueryHandler> logger)
    : IQueryHandler<ListFeaturedToursQuery, IReadOnlyList<TourSummaryDto>>
{
    private const int FeaturedLimit = 20;

    public async Task<Result<IReadOnlyList<TourSummaryDto>>> Handle(
        ListFeaturedToursQuery query, CancellationToken ct)
    {
        // SQL-level Take via IQueryable — no full-table load
        var dtos = await tourRepo
            .Query(asNoTracking: true)
            .Where(t => t.IsFeatured && t.Status == TourStatus.Approved && !t.IsDeleted)
            .OrderByDescending(t => t.BookingCount)
            .ThenByDescending(t => t.AverageRating)
            .Take(FeaturedLimit)
            .Select(t => new TourSummaryDto(
                t.Id, t.Name, t.Slug,
                t.BasePrice.Amount, t.Currency, t.SalePrice,
                t.AverageRating, t.ReviewCount, t.BookingCount,
                t.IsFeatured, t.Status.ToString(), t.CreatedAt))
            .ToListAsync(ct);

        logger.LogDebug("Listed {Count} featured tours", dtos.Count);

        return Result.Success((IReadOnlyList<TourSummaryDto>)dtos);
    }
}
