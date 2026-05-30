using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetSimilarEntities;

public sealed class GetSimilarEntitiesQueryHandler(
    ISuggestionBatchRepository batchRepository,
    IRecommendationCacheRepository cacheRepository,
    IEntityAttributeSnapshotRepository snapshotRepository,
    IEditorialPinRepository pinRepository,
    IBoostPackageRepository boostRepository,
    Scoring.SponsoredAuctionService auctionService,
    ILogger<GetSimilarEntitiesQueryHandler> logger) : IQueryHandler<GetSimilarEntitiesQuery, SimilarEntitiesResponse>
{
    public async Task<Result<SimilarEntitiesResponse>> Handle(GetSimilarEntitiesQuery request, CancellationToken ct)
    {
        var context = request.SourceKind == EntityType.Business ? SuggestionContext.SimilarBusinesses : SuggestionContext.SimilarTours;
        var batch = await batchRepository.GetBySourceAsync(request.SourceKind, request.SourceId, context, ct).ConfigureAwait(false);
        if (batch is null)
        {
            logger.LogDebug("No similar-entity batch found for {SourceKind}/{SourceId}", request.SourceKind, request.SourceId);
            return Result.Success(new SimilarEntitiesResponse([]));
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        var items = await LoadItemsAsync(batch.Id, limit, ct).ConfigureAwait(false);

        // 6.1: Inject sponsored auction winner at position 2 (if available)
        try
        {
            var auctionWinners = await auctionService.RunAuctionAsync(context, request.SourceKind, request.SourceId, 1, ct).ConfigureAwait(false);
            if (auctionWinners.Count > 0)
            {
                var winner = auctionWinners[0];
                if (items.All(i => i.Id != winner.Bid.EntityId))
                {
                    var winnerSnapshot = await snapshotRepository.GetByEntityAsync(winner.Bid.EntityKind, winner.Bid.EntityId, ct).ConfigureAwait(false);
                    if (winnerSnapshot is not null)
                    {
                        var sponsoredItem = RecommendationMapping.ToDto(winnerSnapshot, winner.QualityAdjustedBid, """["boosted","sponsored"]""", isBoosted: true);
                        var insertAt = Math.Min(1, items.Count); // position 2 (0-based index 1)
                        items.Insert(insertAt, sponsoredItem);
                        if (items.Count > limit) items.RemoveAt(items.Count - 1);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sponsored auction failed for {Context}/{SourceKind}/{SourceId} — continuing without sponsored placement", context, request.SourceKind, request.SourceId);
        }

        // 3.7: Inject editorial pins
        var pins = await pinRepository.GetActiveByContextAsync(context, ct).ConfigureAwait(false);
        items = await EditorialPinInjector.InjectPins(items, pins, snapshotRepository, limit, ct).ConfigureAwait(false);

        logger.LogDebug("Read {Count} similar entities for {SourceKind}/{SourceId}", items.Count, request.SourceKind, request.SourceId);
        return Result.Success(new SimilarEntitiesResponse(items));
    }

    private async Task<List<RecommendationItemDto>> LoadItemsAsync(Guid batchId, int limit, CancellationToken ct)
    {
        var rows = await cacheRepository.GetByBatchIdAsync(batchId, ct).ConfigureAwait(false);
        var orderedRows = rows.OrderBy(row => row.Position).Take(limit).ToList();

        // Bulk-load all referenced snapshots to avoid N+1
        var entityKeys = orderedRows.Select(r => (r.EntityKind, r.EntityId)).Distinct();
        var snapshotMap = await snapshotRepository.GetByEntitiesAsync(entityKeys, ct).ConfigureAwait(false);

        var items = new List<RecommendationItemDto>(orderedRows.Count);
        foreach (var row in orderedRows)
        {
            if (snapshotMap.TryGetValue((row.EntityKind, row.EntityId), out var snapshot))
                items.Add(RecommendationMapping.ToDto(snapshot, row.Score, row.SignalsJson));
        }

        return items;
    }
}
