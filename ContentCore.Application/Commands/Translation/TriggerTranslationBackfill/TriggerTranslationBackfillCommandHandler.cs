using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;
using SpecializationEntity = ContentCore.Domain.Entities.Specialization;
using TagEntity = ContentCore.Domain.Entities.Tag;

namespace ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;

/// <summary>
/// Iterates all existing Tag or Specialization rows and triggers auto-translation
/// for any that have zero translations. Safe to call multiple times — skips rows that
/// already have translations for a given language.
///
/// <para>⚠️ This is a long-running admin operation. In production, consider moving
/// it to a background job if the row count is large (1000+).</para>
/// </summary>
public sealed class TriggerTranslationBackfillCommandHandler(
    ITagRepository tagRepository,
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    IEntityTranslationOrchestrator orchestrator,
    ILogger<TriggerTranslationBackfillCommandHandler> logger)
    : ICommandHandler<TriggerTranslationBackfillCommand, TriggerTranslationBackfillResult>
{
    public async Task<Result<TriggerTranslationBackfillResult>> Handle(
        TriggerTranslationBackfillCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return request.EntityKind.ToLowerInvariant() switch
            {
                "tag"            => await BackfillTagsAsync(cancellationToken),
                "specialization" => await BackfillSpecializationsAsync(cancellationToken),
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

    // ── Tags ─────────────────────────────────────────────────────────────────

    private async Task<Result<TriggerTranslationBackfillResult>> BackfillTagsAsync(
        CancellationToken ct)
    {
        var tags = await tagRepository.GetAllAsync(
            include: q => q.Include(t => t.Translations),
            ct: ct);

        var totalAdded = 0;
        var skipped = 0;

        foreach (var tag in tags)
        {
            var added = await TranslateTagAsync(tag, ct);
            if (added == 0) skipped++;
            else totalAdded += added;
        }

        // Single save for all changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Tag translation backfill complete: {Total} tags, {Added} translations added, {Skipped} skipped.",
            tags.Count, totalAdded, skipped);

        return Result<TriggerTranslationBackfillResult>.Success(
            new TriggerTranslationBackfillResult("tag", tags.Count, totalAdded, skipped));
    }

    private async Task<int> TranslateTagAsync(TagEntity tag, CancellationToken ct)
    {
        var fields = new Dictionary<string, string> { ["Name"] = tag.Name };

        var sets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields, tag.SourceLanguageCode, ct);

        var added = 0;
        foreach (var set in sets)
        {
            if (tag.Translations.Any(t => t.LanguageId == set.LanguageId)) continue;
            if (!set.Fields.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name)) continue;

            tag.AddTranslation(set.LanguageId, name, TagEntity.GenerateSlug(name));
            added++;
        }

        return added;
    }

    // ── Specializations ──────────────────────────────────────────────────────

    private async Task<Result<TriggerTranslationBackfillResult>> BackfillSpecializationsAsync(
        CancellationToken ct)
    {
        var specs = await specializationRepository.GetAllAsync(
            include: q => q.Include(s => s.Translations),
            ct: ct);

        var totalAdded = 0;
        var skipped = 0;

        foreach (var spec in specs)
        {
            var added = await TranslateSpecializationAsync(spec, ct);
            if (added == 0) skipped++;
            else totalAdded += added;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Specialization translation backfill complete: {Total} specs, {Added} translations added, {Skipped} skipped.",
            specs.Count, totalAdded, skipped);

        return Result<TriggerTranslationBackfillResult>.Success(
            new TriggerTranslationBackfillResult("specialization", specs.Count, totalAdded, skipped));
    }

    private async Task<int> TranslateSpecializationAsync(SpecializationEntity spec, CancellationToken ct)
    {
        var fields = new Dictionary<string, string> { ["Name"] = spec.Name };
        if (!string.IsNullOrWhiteSpace(spec.Description))
            fields["Description"] = spec.Description;

        var sets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields, spec.SourceLanguageCode, ct);

        var added = 0;
        foreach (var set in sets)
        {
            if (spec.Translations.Any(t => t.LanguageId == set.LanguageId)) continue;
            if (!set.Fields.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name)) continue;

            set.Fields.TryGetValue("Description", out var desc);
            spec.AddTranslation(set.LanguageId, name, desc);
            added++;
        }

        return added;
    }
}
