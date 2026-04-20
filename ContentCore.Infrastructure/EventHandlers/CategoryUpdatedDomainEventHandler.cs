using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.EventHandlers;

public sealed class CategoryUpdatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ICategoryRepository categoryRepository,
    ILogger<CategoryUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CategoryUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CategoryUpdatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var category = await categoryRepository.GetAsync(
            c => c.Id == evt.CategoryId,
            include: q => q.Include(c => c.Translations),
            asNoTracking: false,
            ct: ct);

        if (category is null)
        {
            logger.LogWarning(
                "CategoryUpdatedDomainEvent: Category {CategoryId} not found; skipping re-translation.",
                evt.CategoryId);
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

            // Slug generation delegates to the domain entity's canonical rule.
            var slug = Category.GenerateSlug(translatedName);
            var existing = category.Translations.FirstOrDefault(t => t.LanguageId == set.LanguageId);

            if (existing is not null)
            {
                // Re-translate existing translation ONLY if it's auto-generated.
                // Human-reviewed translations are protected from overwrite.
                if (category.TryUpdateAutoTranslation(set.LanguageId, translatedName, slug))
                {
                    updatedCount++;
                }
                else
                {
                    skippedHumanReviewedCount++;
                }
            }
            else
            {
                // Add translation for any new active language
                category.AddTranslation(set.LanguageId, translatedName, slug);
                addedCount++;
            }
        }

        if (updatedCount == 0 && addedCount == 0)
            return;

        logger.LogInformation(
            "CategoryUpdatedDomainEvent: Updated {UpdatedCount}, added {AddedCount}, skipped {SkippedHumanReviewed} (human-reviewed) for category {CategoryId}.",
            updatedCount,
            addedCount,
            skippedHumanReviewedCount,
            category.Id);
    }
}
