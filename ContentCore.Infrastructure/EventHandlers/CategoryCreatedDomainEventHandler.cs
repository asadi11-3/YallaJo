using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.EventHandlers;

public sealed class CategoryCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ICategoryRepository categoryRepository,
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

            // Slug generation delegates to the domain entity's canonical rule.
            category.AddTranslation(
                set.LanguageId,
                translatedName,
                Category.GenerateSlug(translatedName));

            addedTranslations++;
        }

        if (addedTranslations == 0)
            return;

        // No explicit Update() call needed — EF ChangeTracker detects changes

        logger.LogInformation(
            "CategoryCreatedDomainEvent: Added {TranslationCount} translations for category {CategoryId}.",
            addedTranslations,
            category.Id);
    }
}
