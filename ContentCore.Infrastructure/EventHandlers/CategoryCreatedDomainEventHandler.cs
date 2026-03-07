using System.Text.RegularExpressions;
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
    IContentCoreUnitOfWork unitOfWork,
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
            {
                continue;
            }

            if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
            {
                continue;
            }

            category.AddTranslation(
                Guid.CreateVersion7(),
                set.LanguageId,
                translatedName,
                Slugify(translatedName));

            addedTranslations++;
        }

        if (addedTranslations == 0)
        {
            return;
        }

        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "CategoryCreatedDomainEvent: Added {TranslationCount} translations for category {CategoryId}.",
            addedTranslations,
            category.Id);
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^\w\-]", string.Empty, RegexOptions.None);
        normalized = Regex.Replace(normalized, @"-{2,}", "-");

        return normalized.Trim('-');
    }
}
