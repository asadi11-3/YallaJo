using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles CategoryUpdatedDomainEvent:
///   1. Re-translates auto-translated names to all active languages (human-reviewed are protected).
///   2. Publishes <see cref="CategoryUpdatedIntegrationEvent"/> to the outbox so downstream
///      modules (e.g. ContentSeo) can invalidate cached slugs.
/// </summary>
public sealed class CategoryUpdatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ICategoryRepository categoryRepository,
    ContentCoreDbContext dbContext,
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

        // Publish integration event to outbox (no SaveChanges — UoW commits atomically)
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CategoryUpdatedIntegrationEvent(
                category.Id,
                category.Name,
                category.Slug,
                evt.SourceLanguageCode)));

        logger.LogInformation(
            "CategoryUpdatedDomainEvent: Updated {UpdatedCount}, added {AddedCount}, skipped {SkippedHumanReviewed} (human-reviewed), queued outbox for category {CategoryId}.",
            updatedCount,
            addedCount,
            skippedHumanReviewedCount,
            category.Id);
    }
}
