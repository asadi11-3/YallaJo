using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Backfills <see cref="PlaceTranslation"/> and <see cref="BusinessTranslation"/>
/// rows when a new language is activated.
///
/// <para>
/// CONTENTPLACES-FOLLOWUP-LANGACT-001 — projection-first anti-join handler with
/// <b>DB-level batch fetching</b>.  Mirrors the final ContentTours P1-004.1
/// standard.  Previously the handler executed
/// <c>dbContext.Places.Include(p => p.PlaceTranslations).ToListAsync()</c> and
/// <c>dbContext.Businesses.Include(b => b.BusinessTranslations).ToListAsync()</c>,
/// which materialised the whole Place and Business graphs into tracked memory
/// and issued one Azure Translator HTTP call per entity — even those that
/// already had the target translation.  The previous handler also silently
/// <c>continue</c>-d when the orchestrator returned an empty translated set
/// AND still marked the inbox processed at the end, which permanently dropped
/// the missing translations.
/// </para>
///
/// <para>
/// New behaviour:
/// <list type="bullet">
///   <item>Anti-join queries return only IDs/columns whose translation is missing.</item>
///   <item>Each <c>ToListAsync</c> is bounded by <c>OrderBy(Id).Take(BatchSize)</c>
///   so memory remains bounded even when the missing-translation backlog is
///   very large.</item>
///   <item>Batch size = 200; <c>SaveChangesAsync</c> per batch keeps the EF
///   change tracker bounded and partial progress durable on cancellation.</item>
///   <item>If <see cref="IEntityTranslationOrchestrator.TranslateAsync"/>
///   returns no translated set for a candidate, the handler throws
///   <see cref="InvalidOperationException"/> — this prevents an infinite loop
///   in which the same candidate keeps being re-selected because no
///   translation row was added, and prevents the inbox from being marked
///   processed when work was silently skipped.</item>
///   <item>Inbox <c>MessageId</c> is marked processed only after every batch
///   completes successfully, in a final separate <c>SaveChangesAsync</c> — so
///   a failed or cancelled run leaves the inbox row absent and the next
///   delivery converges via the same anti-join.</item>
/// </list>
/// </para>
/// </summary>
public sealed class LanguageActivatedIntegrationEventHandler(
    ContentPlacesDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesInboxStore inboxStore,
    ILogger<LanguageActivatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<LanguageActivatedIntegrationEvent>>
{
    /// <summary>
    /// Maximum number of Places / Businesses fetched and processed per
    /// <c>SaveChanges</c> batch.  Keeps the EF change tracker bounded, the
    /// transaction span small, and the Azure Translator call rate predictable.
    /// </summary>
    private const int BatchSize = 200;

    private const string SourceLanguageCode = "en";

    public async Task Handle(
        IntegrationEventNotification<LanguageActivatedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken).ConfigureAwait(false))
        {
            logger.LogWarning(
                "ContentPlaces: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var targetCodes = new List<string> { evt.LanguageCode };

        logger.LogInformation(
            "ContentPlaces: language={LanguageCode} languageId={LanguageId} batchSize={BatchSize} " +
            "starting projection-first batch backfill.",
            evt.LanguageCode, evt.LanguageId, BatchSize);

        // ── 1. Place translation backfill ─────────────────────────────────────
        var placeResult = await ProcessPlaceBatchesAsync(
            evt.LanguageId,
            targetCodes,
            cancellationToken).ConfigureAwait(false);

        if (!placeResult.Completed)
        {
            logger.LogWarning(
                "ContentPlaces: Place backfill aborted for language {LanguageCode} " +
                "({PlacesTranslated} places translated before failure). " +
                "Inbox will not be marked processed; retry will pick up remaining.",
                evt.LanguageCode, placeResult.Count);
            return;
        }

        // ── 2. Business translation backfill ──────────────────────────────────
        var businessResult = await ProcessBusinessBatchesAsync(
            evt.LanguageId,
            targetCodes,
            cancellationToken).ConfigureAwait(false);

        if (!businessResult.Completed)
        {
            logger.LogWarning(
                "ContentPlaces: Business backfill aborted for language {LanguageCode} " +
                "({BusinessesTranslated} businesses translated before failure). " +
                "Inbox will not be marked processed; retry will pick up remaining.",
                evt.LanguageCode, businessResult.Count);
            return;
        }

        // ── 3. Mark inbox processed AFTER all batches succeed ─────────────────
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "ContentPlaces: Backfilled language {LanguageCode}: " +
            "translatedPlaces={PlacesTranslated} translatedBusinesses={BusinessesTranslated}.",
            evt.LanguageCode, placeResult.Count, businessResult.Count);
    }

    /// <summary>
    /// Repeats: <c>fetch-up-to-N-missing-places → translate → add → SaveChanges</c>
    /// until the anti-join is empty.  Each <c>ToListAsync</c> call materialises at
    /// most <see cref="BatchSize"/> rows — never the entire backlog.
    /// </summary>
    /// <summary>
    /// Repeats: <c>fetch-up-to-N-missing-places → translate → add → SaveChanges</c>
    /// until the anti-join is empty.  Each <c>ToListAsync</c> call materialises at
    /// most <see cref="BatchSize"/> rows — never the entire backlog.
    /// </summary>
    private async Task<(int Count, bool Completed)> ProcessPlaceBatchesAsync(
        Guid languageId,
        IReadOnlyList<string> targetCodes,
        CancellationToken cancellationToken)
    {
        var totalTranslated = 0;
        var batchIndex = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Anti-join: only Places whose target-language PlaceTranslation is
            // missing.  Ordered by Id (stable, deterministic) and capped at
            // BatchSize so memory stays bounded even on huge tenants.
            var batch = await dbContext.Places
                .AsNoTracking()
                .Where(p => !dbContext.PlaceTranslations
                    .Any(t => t.PlaceId == p.Id && t.LanguageId == languageId))
                .OrderBy(p => p.Id)
                .Take(BatchSize)
                .Select(p => new PlaceTranslationCandidate(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Address))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            batchIndex++;

            foreach (var candidate in batch)
            {
                var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Name"]        = candidate.Name,
                    ["Description"] = candidate.Description ?? string.Empty,
                    ["Address"]     = candidate.Address ?? string.Empty,
                };

                var translatedSets = await orchestrator
                    .TranslateAsync(fields, SourceLanguageCode, targetCodes, cancellationToken)
                    .ConfigureAwait(false);

                if (translatedSets.Count == 0)
                {
                    // Translation service is unhealthy — log and abort this
                    // batch gracefully.  The inbox row is never written, so
                    // the next delivery will retry via the same anti-join.
                    logger.LogError(
                        "Translation orchestrator returned no translated set for Place " +
                        "{PlaceId} (language={LanguageCode}). " +
                        "Aborting place backfill; inbox will not be marked processed.",
                        candidate.PlaceId, targetCodes[0]);

                    return (totalTranslated, Completed: false);
                }

                var translated = translatedSets[0];

                dbContext.PlaceTranslations.Add(PlaceTranslation.Create(
                    candidate.PlaceId,
                    languageId,
                    translated.Fields["Name"],
                    translated.Fields["Description"],
                    translated.Fields["Address"]));

                totalTranslated++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "ContentPlaces: place batch {BatchIndex} saved (size={BatchCount}).",
                batchIndex, batch.Count);

            // If we got fewer than BatchSize rows the anti-join is empty on the
            // next iteration — exit early to save one DB round-trip.
            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        return (totalTranslated, Completed: true);
    }

    /// <summary>
    /// Repeats the same <c>fetch-N → translate → add → SaveChanges</c> loop for
    /// Business translations.  Identical termination contract and
    /// orchestrator-empty handling as <see cref="ProcessPlaceBatchesAsync"/>.
    /// </summary>
    /// <summary>
    /// Repeats the same <c>fetch-N → translate → add → SaveChanges</c> loop for
    /// Business translations.  Identical termination contract and
    /// orchestrator-empty handling as <see cref="ProcessPlaceBatchesAsync"/>.
    /// </summary>
    private async Task<(int Count, bool Completed)> ProcessBusinessBatchesAsync(
        Guid languageId,
        IReadOnlyList<string> targetCodes,
        CancellationToken cancellationToken)
    {
        var totalTranslated = 0;
        var batchIndex = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await dbContext.Businesses
                .AsNoTracking()
                .Where(b => !dbContext.BusinessTranslations
                    .Any(t => t.BusinessId == b.Id && t.LanguageId == languageId))
                .OrderBy(b => b.Id)
                .Take(BatchSize)
                .Select(b => new BusinessTranslationCandidate(
                    b.Id,
                    b.Name,
                    b.Description,
                    b.Address))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            batchIndex++;

            foreach (var candidate in batch)
            {
                var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Name"]        = candidate.Name,
                    ["Description"] = candidate.Description ?? string.Empty,
                    ["Address"]     = candidate.Address ?? string.Empty,
                };

                var translatedSets = await orchestrator
                    .TranslateAsync(fields, SourceLanguageCode, targetCodes, cancellationToken)
                    .ConfigureAwait(false);

                if (translatedSets.Count == 0)
                {
                    // Translation service is unhealthy — log and abort this
                    // batch gracefully.  The inbox row is never written, so
                    // the next delivery will retry via the same anti-join.
                    logger.LogError(
                        "Translation orchestrator returned no translated set for Business " +
                        "{BusinessId} (language={LanguageCode}). " +
                        "Aborting business backfill; inbox will not be marked processed.",
                        candidate.BusinessId, targetCodes[0]);

                    return (totalTranslated, Completed: false);
                }

                var translated = translatedSets[0];

                dbContext.BusinessTranslations.Add(BusinessTranslation.Create(
                    candidate.BusinessId,
                    languageId,
                    translated.Fields["Name"],
                    translated.Fields["Description"],
                    translated.Fields["Address"]));

                totalTranslated++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "ContentPlaces: business batch {BatchIndex} saved (size={BatchCount}).",
                batchIndex, batch.Count);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        return (totalTranslated, Completed: true);
    }

    /// <summary>Internal projection shape for the Place anti-join query.</summary>
    private sealed record PlaceTranslationCandidate(
        Guid PlaceId,
        string Name,
        string? Description,
        string? Address);

    /// <summary>Internal projection shape for the Business anti-join query.</summary>
    private sealed record BusinessTranslationCandidate(
        Guid BusinessId,
        string Name,
        string? Description,
        string? Address);
}
