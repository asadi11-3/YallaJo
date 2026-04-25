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
/// Handles TagCreatedDomainEvent:
/// Auto-translates the tag name to all active languages (mirrors CategoryCreatedDomainEventHandler).
/// Translation failure is non-fatal — logged and skipped per-language.
/// </summary>
public sealed class TagCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ITagRepository tagRepository,
    ILogger<TagCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TagCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TagCreatedDomainEvent> notification,
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
                "TagCreatedDomainEvent: Tag {TagId} not found; skipping translation.",
                evt.TagId);
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
}
