using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.ListMyTours;

public sealed class ListMyToursQueryHandler(
    ITourRepository tourRepo,
    ILogger<ListMyToursQueryHandler> logger)
    : IQueryHandler<ListMyToursQuery, ListMyToursResult>
{
    public async Task<Result<ListMyToursResult>> Handle(
        ListMyToursQuery query, CancellationToken ct)
    {
        try
        {
        // Parse StatusFilter string once in-process — Enum.ToString() is NOT EF-translatable
        TourStatus? statusFilter = null;
        if (query.StatusFilter is not null)
        {
            if (!Enum.TryParse<TourStatus>(query.StatusFilter, ignoreCase: true, out var parsed))
                return Result.Invalid<ListMyToursResult>(
                    new Error("Tour.InvalidStatusFilter",
                        $"'{query.StatusFilter}' is not a valid status. " +
                        $"Valid values: {string.Join(", ", Enum.GetNames<TourStatus>())}."));
            statusFilter = parsed;
        }

        // Build SQL-translatable IQueryable — all predicates use enum comparisons, no ToString()
        var q = tourRepo.Query(asNoTracking: true)
            .Where(t => t.CreatedByUserId == query.EffectiveUserId
                     && (query.IncludeDeleted || !t.IsDeleted)
                     && (statusFilter == null || t.Status == statusFilter.Value))
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt);

        var total = await q.CountAsync(ct);

        // Project Status as byte (SQL-safe), convert to name in-process after materialisation
        var raw = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug,
                BasePrice = t.BasePrice.Amount, t.Currency, t.SalePrice,
                t.AverageRating, t.ReviewCount, t.BookingCount,
                t.IsFeatured,
                StatusByte = (byte)t.Status,   // byte is SQL-safe; ToString() is not
                t.CreatedAt
            })
            .ToListAsync(ct);

        var dtos = raw
            .Select(r => new TourSummaryDto(
                r.Id, r.Name, r.Slug,
                r.BasePrice, r.Currency, r.SalePrice,
                r.AverageRating, r.ReviewCount, r.BookingCount,
                r.IsFeatured,
                ((TourStatus)r.StatusByte).ToString(),   // safe: in-process
                r.CreatedAt))
            .ToList() as IReadOnlyList<TourSummaryDto>;

        var totalPages = query.PageSize > 0
            ? (int)Math.Ceiling((double)total / query.PageSize)
            : 0;

        logger.LogDebug(
            "ListMyTours for UserId={UserId}: {Total} total, page {Page}/{TotalPages}",
            query.EffectiveUserId, total, query.Page, totalPages);

        return Result.Success(new ListMyToursResult(dtos, total, query.Page, query.PageSize, totalPages));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<ListMyToursResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
