using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetRatingSummariesBatch;

/// <summary>
/// [Backend] B6 — one grouped DB query producing per-entity rating aggregates.
/// </summary>
internal sealed class GetRatingSummariesBatchQueryHandler(
    IReviewRepository reviewRepository,
    ILogger<GetRatingSummariesBatchQueryHandler> logger)
    : IQueryHandler<GetRatingSummariesBatchQuery, IReadOnlyList<RatingSummaryBatchItemDto>>
{
    private const int MaxIds = 50;

    public async Task<Result<IReadOnlyList<RatingSummaryBatchItemDto>>> Handle(
        GetRatingSummariesBatchQuery query, CancellationToken ct)
    {
        if (query.EntityIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<RatingSummaryBatchItemDto>>([]);
        }

        if (query.EntityIds.Count > MaxIds)
        {
            return Result.Failure<IReadOnlyList<RatingSummaryBatchItemDto>>(
                new Error("Review.TooManyEntityIds", $"At most {MaxIds} entityIds are allowed per batch."),
                Outcome.Invalid);
        }

        var ids = query.EntityIds.Distinct().ToArray();

        var grouped = await reviewRepository.Query(asNoTracking: true)
            .Where(r => r.TargetType == query.EntityType
                        && ids.Contains(r.TargetId)
                        && r.Status == ReviewStatus.Published)
            .GroupBy(r => r.TargetId)
            .Select(g => new { TargetId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
            .ToListAsync(ct);

        var byId = grouped.ToDictionary(g => g.TargetId);

        var items = ids
            .Select(id => byId.TryGetValue(id, out var g)
                ? new RatingSummaryBatchItemDto(id, decimal.Round(g.Average, 1), g.Count)
                : new RatingSummaryBatchItemDto(id, 0m, 0))
            .ToList();

        logger.LogDebug("Batch rating summary for {IdCount} {EntityType} ids", ids.Length, query.EntityType);

        return Result.Success<IReadOnlyList<RatingSummaryBatchItemDto>>(items);
    }
}
