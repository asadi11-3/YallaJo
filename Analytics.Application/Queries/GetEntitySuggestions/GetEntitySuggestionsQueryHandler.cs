using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetEntitySuggestions;

public sealed class GetEntitySuggestionsQueryHandler(
    ISuggestionBatchRepository batchRepository,
    IRecommendationCacheRepository cacheRepository,
    IEntityAttributeSnapshotRepository snapshotRepository,
    IEditorialPinRepository pinRepository,
    ILogger<GetEntitySuggestionsQueryHandler> logger) : IQueryHandler<GetEntitySuggestionsQuery, EntitySuggestionsResponse>
{
    public async Task<Result<EntitySuggestionsResponse>> Handle(GetEntitySuggestionsQuery request, CancellationToken ct)
    {
        var batch = await batchRepository.GetBySourceAsync(request.SourceKind, request.SourceId, request.Context, ct).ConfigureAwait(false);
        if (batch is null)
        {
            logger.LogDebug("No suggestion batch found for {SourceKind}/{SourceId} context {Context}", request.SourceKind, request.SourceId, request.Context);
            return Result.Success(new EntitySuggestionsResponse([]));
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        var rows = await cacheRepository.GetByBatchIdAsync(batch.Id, ct).ConfigureAwait(false);
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

        // 3.7: Inject editorial pins
        var pins = await pinRepository.GetActiveByContextAsync(request.Context, ct).ConfigureAwait(false);
        items = await EditorialPinInjector.InjectPins(items, pins, snapshotRepository, limit, ct).ConfigureAwait(false);

        logger.LogDebug("Read {Count} entity suggestions for {SourceKind}/{SourceId} context {Context}", items.Count, request.SourceKind, request.SourceId, request.Context);
        return Result.Success(new EntitySuggestionsResponse(items));
    }
}
