using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles SpecializationUpdatedDomainEvent:
/// Re-translates auto-translated Name and Description. Human-reviewed translations are protected.
/// (Mirrors TagUpdatedDomainEventHandler pattern.)
/// </summary>
public sealed class SpecializationUpdatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ISpecializationRepository specializationRepository,
    ILogger<SpecializationUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SpecializationUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SpecializationUpdatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var spec = await specializationRepository.GetAsync(
            s => s.Id == evt.SpecializationId,
            include: q => q.Include(s => s.Translations),
            asNoTracking: false,
            ct: ct);

        if (spec is null)
        {
            logger.LogWarning(
                "SpecializationUpdatedDomainEvent: Specialization {SpecializationId} not found; skipping re-translation.",
                evt.SpecializationId);
            return;
        }

        var fields = new Dictionary<string, string> { ["Name"] = evt.Name };
        if (!string.IsNullOrWhiteSpace(evt.Description))
            fields["Description"] = evt.Description;

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields,
            evt.SourceLanguageCode,
            ct);

        var updatedCount = 0;
        var addedCount = 0;
        var skippedCount = 0;

        foreach (var set in translationSets)
        {
            if (!set.Fields.TryGetValue("Name", out var translatedName) || string.IsNullOrWhiteSpace(translatedName))
                continue;

            set.Fields.TryGetValue("Description", out var translatedDescription);

            var existing = spec.Translations.FirstOrDefault(t => t.LanguageId == set.LanguageId);

            if (existing is not null)
            {
                if (spec.TryUpdateAutoTranslation(set.LanguageId, translatedName, translatedDescription))
                    updatedCount++;
                else
                    skippedCount++;
            }
            else
            {
                spec.AddTranslation(set.LanguageId, translatedName, translatedDescription);
                addedCount++;
            }
        }

        logger.LogInformation(
            "SpecializationUpdatedDomainEvent: Updated {Updated}, added {Added}, skipped {Skipped} (human-reviewed) for specialization {SpecializationId}.",
            updatedCount,
            addedCount,
            skippedCount,
            spec.Id);
    }
}
