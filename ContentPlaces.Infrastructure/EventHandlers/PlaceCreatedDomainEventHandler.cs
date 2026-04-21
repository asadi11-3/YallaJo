using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Triggers auto-translation for all active languages when a Place is created.
/// Runs in the same UoW scope as the command — does NOT call SaveChangesAsync.
/// </summary>
public sealed class PlaceCreatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IEntityTranslationOrchestrator orchestrator,
    ContentPlacesDbContext dbContext,
    ILogger<PlaceCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PlaceCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<PlaceCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        var place = await placeRepository.GetAsync(
            filter:      p => p.Id == evt.PlaceId,
            include:     q => q.Include(p => p.PlaceTranslations),
            asNoTracking: false,
            ct:          cancellationToken);

        if (place is null)
        {
            logger.LogWarning(
                "PlaceCreatedDomainEvent: Place {PlaceId} not found; skipping translation.",
                evt.PlaceId);
            return;
        }

        var fields = new Dictionary<string, string>
        {
            ["Name"] = evt.Name
        };

        if (!string.IsNullOrWhiteSpace(place.Description))
            fields["Description"] = place.Description;

        if (!string.IsNullOrWhiteSpace(place.Address))
            fields["Address"] = place.Address;

        var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
            fields, sourceLanguageCode: "en", cancellationToken);

        var addedCount = 0;

        foreach (var set in translationSets)
        {
            if (place.PlaceTranslations.Any(t => t.LanguageId == set.LanguageId))
                continue;

            if (!set.Fields.TryGetValue("Name", out var translatedName) ||
                string.IsNullOrWhiteSpace(translatedName))
                continue;

            set.Fields.TryGetValue("Description", out var translatedDescription);
            set.Fields.TryGetValue("Address", out var translatedAddress);

            place.AddOrUpdateTranslation(
                set.LanguageId,
                translatedName,
                string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                string.IsNullOrWhiteSpace(translatedAddress) ? null : translatedAddress);

            addedCount++;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new PlaceCreatedIntegrationEvent(evt.PlaceId, evt.Name, evt.Slug)));

        if (addedCount > 0)
        {
            logger.LogInformation(
                "PlaceCreatedDomainEvent: Added {Count} translations for place {PlaceId}.",
                addedCount, evt.PlaceId);
        }
    }
}
