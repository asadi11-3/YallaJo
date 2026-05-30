using ContentTours.Contracts;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Events;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourUpdatedDomainEventHandler(
    ITourRepository tourRepository,
    IEntityTranslationOrchestrator orchestrator,
    ContentToursDbContext dbContext,
    ILogger<TourUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<TourUpdatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        // Always stage the outbox row.
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourUpdatedIntegrationEvent(
                TourId:                  evt.TourId,
                NameChanged:             evt.NameChanged,
                DescriptionChanged:      evt.DescriptionChanged,
                ShortDescriptionChanged: evt.ShortDescriptionChanged,
                PlaceIdChanged:          evt.PlaceIdChanged,
                ChildrenInfoChanged:     evt.ChildrenInfoChanged)));

        // Skip translation work when no identity-bearing field changed.
        if (!evt.NameChanged && !evt.DescriptionChanged && !evt.ShortDescriptionChanged)
        {
            return;
        }

        try
        {
            var tour = await tourRepository.GetAsync(
                filter:       t => t.Id == evt.TourId,
                include:      q => q.Include(t => t.TourTranslations),
                asNoTracking: false,
                ct:           cancellationToken)
                .ConfigureAwait(false);

            if (tour is null)
            {
                logger.LogWarning(
                    "TourUpdatedDomainEvent: tour {TourId} not found; skipping translation refresh.",
                    evt.TourId);
                return;
            }

            var fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Name"] = tour.Name,
            };

            if (!string.IsNullOrWhiteSpace(tour.Description))
                fields["Description"] = tour.Description;

            if (!string.IsNullOrWhiteSpace(tour.ShortDescription))
                fields["ShortDescription"] = tour.ShortDescription;

            var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
                fields, sourceLanguageCode: "en", cancellationToken)
                .ConfigureAwait(false);

            var updatedCount = 0;
            var addedCount   = 0;

            foreach (var set in translationSets)
            {
                if (!set.Fields.TryGetValue("Name", out var translatedName) ||
                    string.IsNullOrWhiteSpace(translatedName))
                    continue;

                set.Fields.TryGetValue("Description", out var translatedDescription);
                set.Fields.TryGetValue("ShortDescription", out var translatedShortDescription);

                var existing = tour.TourTranslations.FirstOrDefault(t => t.LanguageId == set.LanguageId);
                if (existing is not null)
                {
                    // TourTranslation has only a private setter surface; replace the row.
                    dbContext.TourTranslations.Remove(existing);
                    dbContext.TourTranslations.Add(TourTranslation.Create(
                        tourId:           tour.Id,
                        languageId:       set.LanguageId,
                        name:             translatedName,
                        description:      string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                        shortDescription: string.IsNullOrWhiteSpace(translatedShortDescription) ? null : translatedShortDescription));
                    updatedCount++;
                }
                else
                {
                    dbContext.TourTranslations.Add(TourTranslation.Create(
                        tourId:           tour.Id,
                        languageId:       set.LanguageId,
                        name:             translatedName,
                        description:      string.IsNullOrWhiteSpace(translatedDescription) ? null : translatedDescription,
                        shortDescription: string.IsNullOrWhiteSpace(translatedShortDescription) ? null : translatedShortDescription));
                    addedCount++;
                }
            }

            if (updatedCount > 0 || addedCount > 0)
            {
                logger.LogInformation(
                    "TourUpdatedDomainEvent: refreshed {Updated} and added {Added} translations for tour {TourId}.",
                    updatedCount, addedCount, evt.TourId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(
                ex,
                "TourUpdatedDomainEvent: translation refresh failed for tour {TourId}; continuing without refresh.",
                evt.TourId);
        }
    }
}
