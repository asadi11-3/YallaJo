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
/// Handles SpecializationCreatedDomainEvent:
/// Auto-translates Name and Description to all active languages.
/// (Mirrors TagCreatedDomainEventHandler pattern.)
/// </summary>
/// <remarks>
/// Domain events are dispatched before SaveChanges, so the new Specialization is still tracked as
/// <c>Added</c> and not yet queryable from the database. We resolve it from the change tracker
/// (DbSet.Local) rather than via a repository query (which would return null pre-save).
/// </remarks>
public sealed class SpecializationCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentCoreDbContext dbContext,
    ILogger<SpecializationCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SpecializationCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SpecializationCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Resolve the just-created aggregate from the change tracker (state = Added).
        var spec = dbContext.Set<Specialization>().Local.FirstOrDefault(s => s.Id == evt.SpecializationId);

        if (spec is null)
        {
            logger.LogWarning(
                "SpecializationCreatedDomainEvent: Specialization {SpecializationId} not tracked; skipping translation.",
                evt.SpecializationId);
            return;
        }

        try
        {
            var fields = new Dictionary<string, string> { ["Name"] = evt.Name };
            if (!string.IsNullOrWhiteSpace(evt.Description))
                fields["Description"] = evt.Description;

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields,
                evt.SourceLanguageCode,
                ct).ConfigureAwait(false);

            var addedTranslations = 0;

            foreach (var set in translationSets)
            {
                if (spec.Translations.Any(t => t.LanguageId == set.LanguageId))
                    continue;

                if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
                    continue;

                set.Fields.TryGetValue("Description", out var translatedDescription);

                spec.AddTranslation(set.LanguageId, translatedName, translatedDescription);
                addedTranslations++;
            }

            logger.LogInformation(
                "SpecializationCreatedDomainEvent: Added {TranslationCount} translations for specialization {SpecializationId}.",
                addedTranslations,
                spec.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SpecializationCreatedDomainEvent: translation failed for specialization {SpecializationId}; created with source language only.",
                evt.SpecializationId);
        }
    }
}
