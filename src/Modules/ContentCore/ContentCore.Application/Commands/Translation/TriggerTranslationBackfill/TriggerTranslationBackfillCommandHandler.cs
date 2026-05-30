using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;
using TagEntity = ContentCore.Domain.Entities.Tag;

namespace ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;

/// </summary>
public sealed class TriggerTranslationBackfillCommandHandler(
    IContentCoreUnitOfWork unitOfWork,
    IEntityTranslationOrchestrator orchestrator,
    IActiveLanguageProvider activeLanguageProvider,
    ITranslationBackfillStore backfillStore,
    ILogger<TriggerTranslationBackfillCommandHandler> logger)
    : ICommandHandler<TriggerTranslationBackfillCommand, TriggerTranslationBackfillResult>
{
    private const int BatchSize = 200;

    public async Task<Result<TriggerTranslationBackfillResult>> Handle(
        TriggerTranslationBackfillCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return request.EntityKind.ToLowerInvariant() switch
            {
                "tag"            => await BackfillTagsAsync(cancellationToken).ConfigureAwait(false),
                "specialization" => await BackfillSpecializationsAsync(cancellationToken).ConfigureAwait(false),
                _                => Result<TriggerTranslationBackfillResult>.Failure(
                                       new Error("Backfill.InvalidKind", $"Unknown entity kind '{request.EntityKind}'."),
                                       Outcome.Invalid)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TriggerTranslationBackfillResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<Result<TriggerTranslationBackfillResult>> BackfillTagsAsync(
        CancellationToken cancellationToken)
    {
        var activeLanguages = await activeLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken)
            .ConfigureAwait(false);
        var activeLanguageIds = activeLanguages.Select(l => l.Id).ToList();
        // Build an Id → Code lookup so we can request ONLY the missing target
        // codes from the orchestrator and map LanguageCode results back to ids.
        var codeById = activeLanguages.ToDictionary(l => l.Id, l => l.Code);

        logger.LogInformation(
            "Tag translation backfill starting: activeLanguages={Count} batchSize={BatchSize}",
            activeLanguages.Count, BatchSize);

        var totalProcessed = 0;
        var totalAdded = 0;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = await backfillStore
                    .FetchNextTagBackfillCandidatesAsync(activeLanguageIds, BatchSize, cancellationToken)
                    .ConfigureAwait(false);

                if (batch.Count == 0)
                    break;

                var batchIds = batch.Select(c => c.TagId).ToList();
                var existing = await backfillStore
                    .GetExistingTagTranslationLanguageIdsAsync(batchIds, cancellationToken)
                    .ConfigureAwait(false);

                var existingByTag = existing
                    .GroupBy(p => p.EntityId)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.LanguageId).ToHashSet());

                foreach (var candidate in batch)
                {
                    var have = existingByTag.GetValueOrDefault(candidate.TagId, []);
                    var missingIds = activeLanguageIds.Where(id => !have.Contains(id)).ToList();

                    if (missingIds.Count == 0)
                        continue;

                    var missingCodes = missingIds.Select(id => codeById[id]).ToList();

                    var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Name"] = candidate.Name,
                    };

                    var sets = await orchestrator.TranslateAsync(
                        fields, candidate.SourceLanguageCode, missingCodes, cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var set in sets)
                    {
                        if (!set.Fields.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
                            continue;

                        backfillStore.AddTagTranslation(
                            candidate.TagId, set.LanguageId, name, TagEntity.GenerateSlug(name));
                        totalAdded++;
                    }

                    totalProcessed++;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                logger.LogDebug(
                    "Tag backfill: batch saved (size={BatchCount}, addedSoFar={AddedSoFar})",
                    batch.Count, totalAdded);

                // If we got fewer than BatchSize rows the next anti-join would
                // return zero — exit early to save one DB round-trip.
                if (batch.Count < BatchSize)
                    break;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TriggerTranslationBackfillResult>.Conflict(
                new Error(
                    "Translation.ConcurrencyConflict",
                    "One or more records were modified by another user. Please retry."));
        }

        logger.LogInformation(
            "Tag translation backfill complete: {Processed} tags processed, {Added} translations added.",
            totalProcessed, totalAdded);

        return Result<TriggerTranslationBackfillResult>.Success(
            new TriggerTranslationBackfillResult(
                EntityKind:             "tag",
                TotalProcessed:         totalProcessed,
                TotalTranslationsAdded: totalAdded,
                TotalSkipped:           0));
    }

    // ── Specialization backfill ──────────────────────────────────────────────

    private async Task<Result<TriggerTranslationBackfillResult>> BackfillSpecializationsAsync(
        CancellationToken cancellationToken)
    {
        var activeLanguages = await activeLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken)
            .ConfigureAwait(false);
        var activeLanguageIds = activeLanguages.Select(l => l.Id).ToList();
        var codeById = activeLanguages.ToDictionary(l => l.Id, l => l.Code);

        logger.LogInformation(
            "Specialization translation backfill starting: activeLanguages={Count} batchSize={BatchSize}",
            activeLanguages.Count, BatchSize);

        var totalProcessed = 0;
        var totalAdded = 0;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = await backfillStore
                    .FetchNextSpecializationBackfillCandidatesAsync(activeLanguageIds, BatchSize, cancellationToken)
                    .ConfigureAwait(false);

                if (batch.Count == 0)
                    break;

                var batchIds = batch.Select(c => c.SpecializationId).ToList();
                var existing = await backfillStore
                    .GetExistingSpecializationTranslationLanguageIdsAsync(batchIds, cancellationToken)
                    .ConfigureAwait(false);

                var existingBySpec = existing
                    .GroupBy(p => p.EntityId)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.LanguageId).ToHashSet());

                foreach (var candidate in batch)
                {
                    var have = existingBySpec.GetValueOrDefault(candidate.SpecializationId, []);
                    var missingIds = activeLanguageIds.Where(id => !have.Contains(id)).ToList();

                    if (missingIds.Count == 0)
                        continue;

                    var missingCodes = missingIds.Select(id => codeById[id]).ToList();

                    var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["Name"] = candidate.Name,
                    };
                    if (!string.IsNullOrWhiteSpace(candidate.Description))
                        fields["Description"] = candidate.Description;

                    var sets = await orchestrator.TranslateAsync(
                        fields, candidate.SourceLanguageCode, missingCodes, cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var set in sets)
                    {
                        if (!set.Fields.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
                            continue;

                        set.Fields.TryGetValue("Description", out var desc);

                        backfillStore.AddSpecializationTranslation(
                            candidate.SpecializationId, set.LanguageId, name, desc);
                        totalAdded++;
                    }

                    totalProcessed++;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                logger.LogDebug(
                    "Specialization backfill: batch saved (size={BatchCount}, addedSoFar={AddedSoFar})",
                    batch.Count, totalAdded);

                if (batch.Count < BatchSize)
                    break;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TriggerTranslationBackfillResult>.Conflict(
                new Error(
                    "Translation.ConcurrencyConflict",
                    "One or more records were modified by another user. Please retry."));
        }

        logger.LogInformation(
            "Specialization translation backfill complete: {Processed} specs processed, {Added} translations added.",
            totalProcessed, totalAdded);

        return Result<TriggerTranslationBackfillResult>.Success(
            new TriggerTranslationBackfillResult(
                EntityKind:             "specialization",
                TotalProcessed:         totalProcessed,
                TotalTranslationsAdded: totalAdded,
                TotalSkipped:           0));
    }
}
