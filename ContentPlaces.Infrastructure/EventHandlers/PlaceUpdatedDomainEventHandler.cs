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

public sealed class PlaceUpdatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IEntityTranslationOrchestrator orchestrator,
    ContentPlacesDbContext dbContext,
    ILogger<PlaceUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PlaceUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<PlaceUpdatedDomainEvent> notification,
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
            fields, sourceLanguageCode: "en", cancellationToken);

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

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new PlaceUpdatedIntegrationEvent(evt.PlaceId, evt.Name, evt.Description, evt.Address, evt.Slug, evt.OldSlug)));

        logger.LogInformation(
            "PlaceUpdatedDomainEvent: Updated {Updated}, added {Added} translations for place {PlaceId}.",
            updatedCount, addedCount, evt.PlaceId);
    }
}
