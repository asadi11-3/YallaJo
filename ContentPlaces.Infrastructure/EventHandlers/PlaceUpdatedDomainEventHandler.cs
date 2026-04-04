using ContentPlaces.Domain.Events;
using ContentPlaces.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Re-triggers translation when a Place's Name, Description, or Address changes.
/// Runs in the same UoW scope as the command — does NOT call SaveChangesAsync.
/// </summary>
public sealed class PlaceUpdatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IEntityTranslationOrchestrator orchestrator,
    ILogger<PlaceUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PlaceUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<PlaceUpdatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var place = await placeRepository.GetAsync(
            filter:      p => p.Id == evt.PlaceId,
            include:     q => q.Include(p => p.PlaceTranslations),
            asNoTracking: false,
            ct:          ct);

        if (place is null)
        {
            logger.LogWarning(
                "PlaceUpdatedDomainEvent: Place {PlaceId} not found; skipping re-translation.",
                evt.PlaceId);
            return;
        }

        var fields = new Dictionary<string, string> { ["Name"] = evt.Name };

        if (!string.IsNullOrWhiteSpace(evt.Description))
            fields["Description"] = evt.Description;

        if (!string.IsNullOrWhiteSpace(evt.Address))
            fields["Address"] = evt.Address;

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields, sourceLanguageCode: "en", ct);

        var updatedCount = 0;
        var addedCount   = 0;

        foreach (var set in translationSets)
        {
            if (!set.Fields.TryGetValue("Name", out var translatedName) ||
                string.IsNullOrWhiteSpace(translatedName))
                continue;

            set.Fields.TryGetValue("Description", out var translatedDescription);
            set.Fields.TryGetValue("Address", out var translatedAddress);

            var existing = place.PlaceTranslations.FirstOrDefault(t => t.LanguageId == set.LanguageId);
            if (existing is not null)
            {
                place.AddOrUpdateTranslation(
                    set.LanguageId,
                    translatedName,
                    string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                    string.IsNullOrWhiteSpace(translatedAddress) ? null : translatedAddress);
                updatedCount++;
            }
            else
            {
                place.AddOrUpdateTranslation(
                    set.LanguageId,
                    translatedName,
                    string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                    string.IsNullOrWhiteSpace(translatedAddress) ? null : translatedAddress);
                addedCount++;
            }
        }

        logger.LogInformation(
            "PlaceUpdatedDomainEvent: Updated {Updated}, added {Added} translations for place {PlaceId}.",
            updatedCount, addedCount, evt.PlaceId);
    }
}
