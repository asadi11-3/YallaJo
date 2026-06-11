using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.GetMyTourStatusCounts;

/// <summary>
/// [Backend] B1 — single grouped query producing per-status counts for the caller's tours.
/// Mirrors the visibility rules of ListMyTours (excludes soft-deleted tours).
/// </summary>
public sealed class GetMyTourStatusCountsQueryHandler(
    ITourRepository tourRepo,
    ILogger<GetMyTourStatusCountsQueryHandler> logger)
    : IQueryHandler<GetMyTourStatusCountsQuery, TourStatusCountsDto>
{
    public async Task<Result<TourStatusCountsDto>> Handle(
        GetMyTourStatusCountsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var grouped = await tourRepo.Query(asNoTracking: true)
                .Where(t => t.CreatedByUserId == query.EffectiveUserId && !t.IsDeleted)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var counts = grouped.ToDictionary(x => x.Status, x => x.Count);

            int Of(TourStatus s) => counts.TryGetValue(s, out var c) ? c : 0;

            var dto = new TourStatusCountsDto(
                Draft: Of(TourStatus.Draft),
                Pending: Of(TourStatus.Pending),
                Approved: Of(TourStatus.Approved),
                Rejected: Of(TourStatus.Rejected),
                Suspended: Of(TourStatus.Suspended),
                Archived: Of(TourStatus.Archived),
                Total: counts.Values.Sum());

            logger.LogDebug(
                "Tour status counts for UserId={UserId}: total {Total}",
                query.EffectiveUserId, dto.Total);

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TourStatusCountsDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
