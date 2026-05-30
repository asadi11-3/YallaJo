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
/// Handles CategoryCreatedDomainEvent:
///   1. Auto-translates the category name to all active languages.
///   2. Publishes <see cref="CategoryCreatedIntegrationEvent"/> to the outbox so downstream
///      modules (e.g. ContentSeo) can react by auto-creating SeoMetadata.
/// </summary>
public sealed class CategoryCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ICategoryRepository categoryRepository,
    ContentCoreDbContext dbContext,
    ILogger<CategoryCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CategoryCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CategoryCreatedDomainEvent> notification,
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
                "CategoryCreatedDomainEvent: Category {CategoryId} not found; skipping translation.",
                evt.CategoryId);
            return;
        }

        var fields = new Dictionary<string, string> { ["Name"] = evt.Name };

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields,
            evt.SourceLanguageCode,
            ct);

        var addedTranslations = 0;

        foreach (var set in translationSets)
        {
            if (category.Translations.Any(t => t.LanguageId == set.LanguageId))
                continue;

            if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
                continue;

            category.AddTranslation(
                set.LanguageId,
                translatedName,
                Category.GenerateSlug(translatedName));

            addedTranslations++;
        }

        // Publish integration event to outbox (no SaveChanges — UoW commits atomically)
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CategoryCreatedIntegrationEvent(
                category.Id,
                category.Name,
                category.Slug,
                category.ParentCategoryId,
                evt.SourceLanguageCode)));

        logger.LogInformation(
            "CategoryCreatedDomainEvent: Added {TranslationCount} translations and queued outbox for category {CategoryId}.",
            addedTranslations,
            category.Id);
    }
}
