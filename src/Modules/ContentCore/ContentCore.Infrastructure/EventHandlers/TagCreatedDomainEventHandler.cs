using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles TagCreatedDomainEvent:
/// Auto-translates the tag name to all active languages (mirrors CategoryCreatedDomainEventHandler).
/// Translation failure is non-fatal — logged and skipped.
/// </summary>
/// <remarks>
/// Domain events are dispatched before SaveChanges, so the new Tag is still tracked as <c>Added</c>
/// and not yet queryable from the database. We resolve it from the change tracker (DbSet.Local)
/// rather than via a repository query (which would return null pre-save and skip translation).
/// </remarks>
public sealed class TagCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentCoreDbContext dbContext,
    ILogger<TagCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TagCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TagCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Resolve the just-created aggregate from the change tracker (state = Added).
        var tag = dbContext.Set<Tag>().Local.FirstOrDefault(t => t.Id == evt.TagId);

        if (tag is null)
        {
            logger.LogWarning(
                "TagCreatedDomainEvent: Tag {TagId} not tracked; skipping translation.",
                evt.TagId);
            return;
        }

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
                if (tag.Translations.Any(t => t.LanguageId == set.LanguageId))
                    continue;

                if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
                    continue;

                tag.AddTranslation(
                    set.LanguageId,
                    translatedName,
                    Tag.GenerateSlug(translatedName));

                addedTranslations++;
            }

            logger.LogInformation(
                "TagCreatedDomainEvent: Added {TranslationCount} translations for tag {TagId}.",
                addedTranslations,
                tag.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "TagCreatedDomainEvent: translation failed for tag {TagId}; created with source language only.",
                evt.TagId);
        }
    }
}
