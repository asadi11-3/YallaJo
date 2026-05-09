using ContentCore.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Backfills <see cref="TourTranslation"/> and <see cref="TourPricingTierTranslation"/>
/// rows when a new language is activated.
///
/// <para>
/// P1-004 / P1-004.1: projection-first anti-join handler with <b>DB-level batch
/// fetching</b>.  Previously the handler executed
/// <c>dbContext.Tours.Include(...).Include(...).ThenInclude(...).ToListAsync()</c>
/// (P1-004 fix: replaced with a single full <c>ToListAsync()</c> over a projection).
/// This pass (P1-004.1) replaces the remaining full-list materialisation with a
/// loop that fetches at most <see cref="BatchSize"/> rows per <c>ToListAsync</c>
/// call — so memory remains bounded even when the missing-translation backlog
/// is very large.
/// </para>
///
/// <para>
/// Termination contract:
/// <list type="bullet">
///   <item>Each iteration runs the anti-join, ordered by <c>Id</c>, capped at
///   <see cref="BatchSize"/>.</item>
///   <item>After processing a batch, every candidate has either produced a
///   translation row (added to the change tracker) or the handler throws —
///   so the next anti-join excludes the just-processed candidates and shrinks
///   monotonically.</item>
///   <item>Loop exits when the anti-join returns zero rows.</item>
///   <item>If <see cref="IEntityTranslationOrchestrator.TranslateAsync"/>
///   returns no translated set for a candidate, the handler throws
///   <see cref="InvalidOperationException"/> — this prevents an infinite loop
///   in which the same candidate keeps being re-selected because no
///   translation row was added.</item>
/// </list>
/// </para>
///
/// <para>
/// Inbox <c>MessageId</c> is marked processed only after every batch completes
/// successfully, in a final separate <c>SaveChangesAsync</c> — so a failed or
/// cancelled run leaves the inbox row absent and the next delivery converges
/// via the same anti-join.
/// </para>
///
/// <para>
/// Pricing-tier translations preserve the previous (pre-P1-004) behaviour:
/// no Azure call is issued; the tier's source <c>Name</c> / <c>Description</c>
/// are copied verbatim into the new translation row.
/// </para>
/// </summary>
public sealed class LanguageActivatedIntegrationEventHandler(
    ContentToursDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentToursUnitOfWork unitOfWork,
    IContentToursInboxStore inboxStore,
    ILogger<LanguageActivatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<LanguageActivatedIntegrationEvent>>
{
    /// <summary>
    /// Maximum number of tours / pricing tiers fetched and processed per
    /// <c>SaveChanges</c> batch.  Keeps the EF change tracker bounded, the
    /// transaction span small, and the Azure Translator call rate predictable.
    /// </summary>
    private const int BatchSize = 200;

    private const string SourceLanguageCode = "en";

    public async Task Handle(
        IntegrationEventNotification<LanguageActivatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "ContentTours: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var normalizedLanguageCode = evt.LanguageCode.Trim().ToLowerInvariant();
        var targetCodes = new List<string> { normalizedLanguageCode };

        logger.LogInformation(
            "ContentTours: language={LanguageCode} languageId={LanguageId} batchSize={BatchSize} " +
            "starting projection-first batch backfill.",
            normalizedLanguageCode, evt.LanguageId, BatchSize);

        // ── 1. Tour translation backfill ──────────────────────────────────────
        var totalToursTranslated = await ProcessTourBatchesAsync(
            evt.LanguageId,
            targetCodes,
            ct).ConfigureAwait(false);

        // ── 2. Pricing-tier translation backfill ──────────────────────────────
        var totalTiersTranslated = await ProcessPricingTierBatchesAsync(
            normalizedLanguageCode,
            ct).ConfigureAwait(false);

        // ── 3. Mark inbox processed AFTER all batches succeed ─────────────────
        // Final separate SaveChanges; if any batch above threw, the inbox row is
        // never written, so the next delivery picks up via the same anti-join.
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ContentTours: Backfilled language {LanguageCode}: " +
            "translatedTours={ToursTranslated} translatedTiers={TiersTranslated}.",
            normalizedLanguageCode, totalToursTranslated, totalTiersTranslated);
    }

    /// <summary>
    /// Repeats: <c>fetch-up-to-N-missing-tours → translate → add → SaveChanges</c>
    /// until the anti-join is empty.  Each <c>ToListAsync</c> call materialises at
    /// most <see cref="BatchSize"/> rows — never the entire backlog.
    /// </summary>
    private async Task<int> ProcessTourBatchesAsync(
        Guid languageId,
        IReadOnlyList<string> targetCodes,
        CancellationToken ct)
    {
        var totalTranslated = 0;
        var batchIndex = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Anti-join: only tours whose target-language TourTranslation is
            // missing.  Ordered by Id (stable, deterministic) and capped at
            // BatchSize so memory stays bounded even on huge tenants.
            var batch = await dbContext.Tours
                .AsNoTracking()
                .Where(t => !dbContext.TourTranslations
                    .Any(tt => tt.TourId == t.Id && tt.LanguageId == languageId))
                .OrderBy(t => t.Id)
                .Take(BatchSize)
                .Select(t => new TourTranslationCandidate(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.ShortDescription,
                    t.MeetingPoint != null ? (decimal?)t.MeetingPoint.Latitude  : null,
                    t.MeetingPoint != null ? (decimal?)t.MeetingPoint.Longitude : null))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            batchIndex++;

            foreach (var candidate in batch)
            {
                var meetingPointText = candidate.MeetingPointLatitude.HasValue
                    && candidate.MeetingPointLongitude.HasValue
                    ? $"({candidate.MeetingPointLatitude.Value}, {candidate.MeetingPointLongitude.Value})"
                    : string.Empty;

                var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Name"]             = candidate.Name,
                    ["Description"]      = candidate.Description ?? string.Empty,
                    ["ShortDescription"] = candidate.ShortDescription ?? string.Empty,
                    ["MeetingPoint"]     = meetingPointText,
                };

                var translatedSets = await orchestrator
                    .TranslateAsync(fields, SourceLanguageCode, targetCodes, ct)
                    .ConfigureAwait(false);

                var translated = translatedSets.FirstOrDefault();
                if (translated is null)
                {
                    // Critical: do NOT silently skip.  If we did, the next
                    // anti-join iteration would re-select the same tour
                    // forever (no translation row was added → still missing).
                    // Fail clearly so the inbox is left unmarked and replay
                    // can retry once translation service is healthy.
                    throw new InvalidOperationException(
                        $"Translation orchestrator returned no translated set for Tour " +
                        $"{candidate.TourId} (language={targetCodes[0]}). " +
                        "Aborting backfill so the inbox is not marked processed.");
                }

                dbContext.TourTranslations.Add(TourTranslation.Create(
                    candidate.TourId,
                    languageId,
                    translated.Fields["Name"],
                    translated.Fields["Description"],
                    translated.Fields["ShortDescription"],
                    translated.Fields["MeetingPoint"]));

                totalTranslated++;
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogDebug(
                "ContentTours: tour batch {BatchIndex} saved (size={BatchCount}).",
                batchIndex, batch.Count);

            // If we got fewer than BatchSize rows the anti-join is empty on the
            // next iteration — exit early to save one DB round-trip.
            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        return totalTranslated;
    }

    /// <summary>
    /// Repeats the same <c>fetch-N → add → SaveChanges</c> loop for pricing-tier
    /// translations.  Pricing tiers do NOT call the Azure translator —
    /// <c>Name</c> and <c>Description</c> are copied verbatim from the source
    /// row, preserving the historical pre-P1-004 behaviour.
    /// </summary>
    private async Task<int> ProcessPricingTierBatchesAsync(
        string normalizedLanguageCode,
        CancellationToken ct)
    {
        var totalTranslated = 0;
        var batchIndex = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await dbContext.TourPricingTiers
                .AsNoTracking()
                .Where(p => !dbContext.TourPricingTierTranslations
                    .Any(pt => pt.TourPricingTierId == p.Id && pt.LanguageCode == normalizedLanguageCode))
                .OrderBy(p => p.Id)
                .Take(BatchSize)
                .Select(p => new TourPricingTierTranslationCandidate(p.Id, p.Name, p.Description))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            batchIndex++;

            foreach (var candidate in batch)
            {
                dbContext.TourPricingTierTranslations.Add(TourPricingTierTranslation.Create(
                    candidate.TourPricingTierId,
                    normalizedLanguageCode,
                    candidate.Name,
                    candidate.Description));

                totalTranslated++;
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogDebug(
                "ContentTours: tier batch {BatchIndex} saved (size={BatchCount}).",
                batchIndex, batch.Count);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        return totalTranslated;
    }

    /// <summary>Internal projection shape for the tour anti-join query.</summary>
    private sealed record TourTranslationCandidate(
        Guid TourId,
        string Name,
        string? Description,
        string? ShortDescription,
        decimal? MeetingPointLatitude,
        decimal? MeetingPointLongitude);

    /// <summary>Internal projection shape for the pricing-tier anti-join query.</summary>
    private sealed record TourPricingTierTranslationCandidate(
        Guid TourPricingTierId,
        string Name,
        string? Description);
}
