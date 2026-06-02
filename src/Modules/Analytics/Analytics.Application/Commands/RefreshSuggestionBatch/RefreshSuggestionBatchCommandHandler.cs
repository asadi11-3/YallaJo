using System.Text.Json;
using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Scoring;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RefreshSuggestionBatch;

public sealed class RefreshSuggestionBatchCommandHandler(
    IEntityAttributeSnapshotRepository snapshotRepository,
    ISuggestionBatchRepository batchRepository,
    IRecommendationCacheRepository cacheRepository,
    IBoostPackageRepository boostRepository,
    IRecommendationScoringEngine scoringEngine,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<RefreshSuggestionBatchCommandHandler> logger) : ICommandHandler<RefreshSuggestionBatchCommand>
{
    private const string AlgorithmVersion = "v1";

    public async Task<Result> Handle(RefreshSuggestionBatchCommand request, CancellationToken ct)
    {
        var source = await snapshotRepository.GetByEntityAsync(request.SourceKind, request.SourceId, ct).ConfigureAwait(false);
        if (source is null)
            return Result.Failure(new Error("SuggestionSource.NotFound", "Source entity snapshot was not found."), Outcome.NotFound);

        var candidateKind = ResolveCandidateKind(request.SourceKind, request.Context);
        var candidates = await snapshotRepository.GetActiveByKindAsync(candidateKind, ct).ConfigureAwait(false);

        // Pre-load active flat-fee boosts for scoring (CPC handled separately in query handlers via auction)
        var allCpcBids = await boostRepository.GetAllActiveCpcBidsAsync(ct).ConfigureAwait(false);
        var cpcEntityIds = allCpcBids.Select(b => (b.EntityKind, b.EntityId)).ToHashSet();
        var activeBoosts = new Dictionary<(EntityType Kind, Guid Id), BoostPackage>();
        // Check each candidate for a flat-fee boost (not CPC — those go through auction)
        foreach (var candidate in candidates)
        {
            if (cpcEntityIds.Contains((candidate.EntityKind, candidate.EntityId))) continue;
            var boost = await boostRepository.GetActiveForEntityAsync(candidate.EntityKind, candidate.EntityId, ct).ConfigureAwait(false);
            if (boost is not null)
                activeBoosts[(boost.EntityKind, boost.EntityId)] = boost;
        }

        var scored = scoringEngine.Score(new ScoringContext(source, candidates, request.Context, ActiveBoosts: activeBoosts));

        var batch = await batchRepository.GetBySourceAsync(request.SourceKind, request.SourceId, request.Context, ct).ConfigureAwait(false);
        if (batch is null)
        {
            batch = SuggestionBatch.Create(request.SourceKind, request.SourceId, request.Context, AlgorithmVersion, scored.Count, DateTime.UtcNow);
            await batchRepository.AddAsync(batch, ct).ConfigureAwait(false);
        }
        else
        {
            await cacheRepository.DeleteByBatchIdAsync(batch.Id, ct).ConfigureAwait(false);
            batch.MarkRefreshed(scored.Count, AlgorithmVersion);
        }

        var generatedAt = DateTime.UtcNow;
        var expiresAt = generatedAt.AddHours(24);
        var rows = scored.Select((candidate, index) => RecommendationCache.Create(
            userId: null,
            batchId: batch.Id,
            entityKind: candidate.Snapshot.EntityKind,
            entityId: candidate.Snapshot.EntityId,
            score: candidate.Score,
            position: index + 1,
            signalsJson: JsonSerializer.Serialize(candidate.Signals),
            generatedAt: generatedAt,
            expiresAt: expiresAt)).ToList();

        await cacheRepository.AddRangeAsync(rows, ct).ConfigureAwait(false);

        try
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("SuggestionBatch.ConcurrencyConflict", "Suggestion batch was modified concurrently. Please refresh and try again."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync($"analytics:recs:{request.SourceKind}:{request.SourceId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Refreshed {Count} analytics recommendations for {SourceKind}/{SourceId} context {Context}", scored.Count, request.SourceKind, request.SourceId, request.Context);
        return Result.Success();
    }

    private static EntityType ResolveCandidateKind(EntityType sourceKind, SuggestionContext context)
        => context switch
        {
            SuggestionContext.SimilarTours => EntityType.Tour,
            SuggestionContext.SimilarBusinesses or SuggestionContext.SimilarHotels or SuggestionContext.AddAMeal or SuggestionContext.WhereToStay => EntityType.Business,
            SuggestionContext.AddAnActivity => EntityType.Tour,
            _ => sourceKind
        };
}
