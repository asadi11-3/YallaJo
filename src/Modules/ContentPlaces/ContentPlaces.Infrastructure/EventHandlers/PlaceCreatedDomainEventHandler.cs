using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Events;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Handles PlaceCreatedDomainEvent:
///   1. Auto-translates Name/Description/Address into all active languages.
///   2. Publishes <see cref="PlaceCreatedIntegrationEvent"/> to the outbox.
/// </summary>
/// <remarks>
/// Domain events are dispatched before SaveChanges, so the new Place is still tracked as
/// <c>Added</c> and not yet queryable from the database. We resolve it from the change tracker
/// (DbSet.Local) rather than via a repository query (which would return null pre-save and skip
/// both translation and the outbox notification).
/// </remarks>
public sealed class PlaceCreatedDomainEventHandler(
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

        // Resolve the just-created aggregate from the change tracker (state = Added).
        var place = dbContext.Set<Place>().Local.FirstOrDefault(p => p.Id == evt.PlaceId);

        if (place is null)
        {
            logger.LogWarning(
                "PlaceCreatedDomainEvent: Place {PlaceId} not tracked; skipping translation and outbox.",
                evt.PlaceId);
            return;
        }

        // Stage the cross-module integration event first — mandatory; translation is best-effort.
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new PlaceCreatedIntegrationEvent(evt.PlaceId, evt.Name, evt.Slug)));

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["Name"] = evt.Name
            };

            if (!string.IsNullOrWhiteSpace(place.Description))
                fields["Description"] = place.Description;

            if (!string.IsNullOrWhiteSpace(place.Address))
                fields["Address"] = place.Address;

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields, sourceLanguageCode: "en", cancellationToken).ConfigureAwait(false);

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

            if (addedCount > 0)
            {
                logger.LogInformation(
                    "PlaceCreatedDomainEvent: Added {Count} translations for place {PlaceId}.",
                    addedCount, evt.PlaceId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "PlaceCreatedDomainEvent: translation failed for place {PlaceId}; created with source language only.",
                evt.PlaceId);
        }
    }
}
