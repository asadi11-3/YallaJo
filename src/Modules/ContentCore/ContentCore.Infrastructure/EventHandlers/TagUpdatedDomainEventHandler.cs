using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles TagUpdatedDomainEvent:
/// Re-translates auto-translated tag names to all active languages.
/// Human-reviewed translations are protected from overwrite.
/// (Mirrors CategoryUpdatedDomainEventHandler pattern.)
/// </summary>
public sealed class TagUpdatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ITagRepository tagRepository,
    ILogger<TagUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TagUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TagUpdatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var tag = await tagRepository.GetAsync(
            t => t.Id == evt.TagId,
            include: q => q.Include(t => t.Translations),
            asNoTracking: false,
            ct: ct);

        if (tag is null)
        {
            logger.LogWarning(
                "TagUpdatedDomainEvent: Tag {TagId} not found; skipping re-translation.",
                evt.TagId);
            return;
        }

        var fields = new Dictionary<string, string> { ["Name"] = evt.Name };

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields,
            evt.SourceLanguageCode,
            ct);

        var updatedCount = 0;
        var addedCount = 0;
        var skippedHumanReviewedCount = 0;

        foreach (var set in translationSets)
        {
            if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
                continue;

            var slug = Tag.GenerateSlug(translatedName);
            var existing = tag.Translations.FirstOrDefault(t => t.LanguageId == set.LanguageId);

            if (existing is not null)
            {
                if (tag.TryUpdateAutoTranslation(set.LanguageId, translatedName, slug))
                    updatedCount++;
                else
                    skippedHumanReviewedCount++;
            }
            else
            {
                tag.AddTranslation(set.LanguageId, translatedName, slug);
                addedCount++;
            }
        }

        logger.LogInformation(
            "TagUpdatedDomainEvent: Updated {Updated}, added {Added}, skipped {SkippedHuman} (human-reviewed) for tag {TagId}.",
            updatedCount,
            addedCount,
            skippedHumanReviewedCount,
            tag.Id);
    }
}
