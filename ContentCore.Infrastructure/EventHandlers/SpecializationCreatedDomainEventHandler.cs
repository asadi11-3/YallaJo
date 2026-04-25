using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
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
public sealed class SpecializationCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ISpecializationRepository specializationRepository,
    ILogger<SpecializationCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SpecializationCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<SpecializationCreatedDomainEvent> notification,
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
                "SpecializationCreatedDomainEvent: Specialization {SpecializationId} not found; skipping translation.",
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
}
