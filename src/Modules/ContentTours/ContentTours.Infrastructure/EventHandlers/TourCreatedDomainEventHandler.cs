using ContentTours.Contracts;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    ContentToursDbContext dbContext,
    ILogger<TourCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TourCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        // Always stage the outbox row first — translation is best-effort but cross-module
        // notification is mandatory.
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourCreatedIntegrationEvent(
                TourId:          evt.TourId,
                Name:            evt.Name,
                Slug:            evt.Slug,
                CreatedByUserId: evt.CreatedByUserId,
                PlaceId:         evt.PlaceId)));

        try
        {
            // Build from the domain-event payload only (no repository/DB reload): domain
            // events are dispatched before SaveChanges, so the newly created Tour may still
            // be tracked as Added and not yet queryable from the database.
            var fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Name"] = evt.Name,
            };

            if (!string.IsNullOrWhiteSpace(evt.Description))
                fields["Description"] = evt.Description;

            if (!string.IsNullOrWhiteSpace(evt.ShortDescription))
                fields["ShortDescription"] = evt.ShortDescription;

            if (!string.IsNullOrWhiteSpace(evt.MetaTitle))
                fields["MetaTitle"] = evt.MetaTitle;

            if (!string.IsNullOrWhiteSpace(evt.MetaDescription))
                fields["MetaDescription"] = evt.MetaDescription;

            // TODO: read source language from ContentToursOptions/default language configuration.
            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields, sourceLanguageCode: "en", cancellationToken)
                .ConfigureAwait(false);

            var existingLanguageIds = dbContext.TourTranslations.Local
                .Where(t => t.TourId == evt.TourId)
                .Select(t => t.LanguageId)
                .ToHashSet();

            var addedCount = 0;
            foreach (var set in translationSets)
            {
                if (!existingLanguageIds.Add(set.LanguageId))
                    continue;

                if (!set.Fields.TryGetValue("Name", out var translatedName) ||
                    string.IsNullOrWhiteSpace(translatedName))
                    continue;

                set.Fields.TryGetValue("Description", out var translatedDescription);
                set.Fields.TryGetValue("ShortDescription", out var translatedShortDescription);

                dbContext.TourTranslations.Add(TourTranslation.Create(
                    tourId:           evt.TourId,
                    languageId:       set.LanguageId,
                    name:             translatedName,
                    description:      string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                    shortDescription: string.IsNullOrWhiteSpace(translatedShortDescription) ? null : translatedShortDescription));

                addedCount++;
            }

            if (addedCount > 0)
            {
                logger.LogInformation(
                    "TourCreatedDomainEvent: staged {Count} translations for tour {TourId}.",
                    addedCount, evt.TourId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Let cancellation propagate — the command handler's outer guard will surface it.
            throw;
        }
#pragma warning disable CA1031 // Do not catch general exception types — translation must not fail the command.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(
                ex,
                "TourCreatedDomainEvent: translation orchestration failed for tour {TourId}; continuing without translations. " +
                "Backfill will run on the next LanguageActivated event or via a manual re-translation pass.",
                evt.TourId);
        }
    }
}
