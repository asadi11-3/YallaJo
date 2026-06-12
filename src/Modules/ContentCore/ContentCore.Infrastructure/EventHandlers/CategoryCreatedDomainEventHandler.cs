using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
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
/// <remarks>
/// Domain events are dispatched <b>before</b> SaveChanges, so the newly created Category is still
/// tracked as <c>Added</c> and is NOT yet queryable from the database. We therefore resolve the
/// aggregate from the change tracker (DbSet.Local) instead of reloading it via a repository query
/// (which returns null pre-save and would silently skip translation + outbox).
/// </remarks>
public sealed class CategoryCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentCoreDbContext dbContext,
    ILogger<CategoryCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CategoryCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CategoryCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Resolve the just-created aggregate from the change tracker (state = Added).
        var category = dbContext.Set<Category>().Local.FirstOrDefault(c => c.Id == evt.CategoryId);

        if (category is null)
        {
            logger.LogWarning(
                "CategoryCreatedDomainEvent: Category {CategoryId} not tracked; skipping translation and outbox.",
                evt.CategoryId);
            return;
        }

        // Stage the cross-module integration event first — it is mandatory; translation is best-effort.
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CategoryCreatedIntegrationEvent(
                category.Id,
                category.Name,
                category.Slug,
                category.ParentCategoryId,
                evt.SourceLanguageCode)));

        try
        {
            var fields = new Dictionary<string, string> { ["Name"] = evt.Name };

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields,
                evt.SourceLanguageCode,
                ct).ConfigureAwait(false);

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

            logger.LogInformation(
                "CategoryCreatedDomainEvent: Added {TranslationCount} translations and queued outbox for category {CategoryId}.",
                addedTranslations,
                category.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "CategoryCreatedDomainEvent: translation failed for category {CategoryId}; created with source language only. " +
                "Backfill will run on the next LanguageActivated event.",
                category.Id);
        }
    }
}
