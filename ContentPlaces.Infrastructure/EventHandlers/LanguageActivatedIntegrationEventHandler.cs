using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class LanguageActivatedIntegrationEventHandler(
    ContentPlacesDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesInboxStore inboxStore,
    ILogger<LanguageActivatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<LanguageActivatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<LanguageActivatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "ContentPlaces: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var targetCodes = new List<string> { evt.LanguageCode };

        var places = await dbContext.Places
            .Include(p => p.PlaceTranslations)
            .ToListAsync(ct);

        foreach (var place in places)
        {
            if (place.PlaceTranslations.Any(t => t.LanguageId == evt.LanguageId))
            {
                continue;
            }

            var fields = new Dictionary<string, string>
            {
                ["Name"] = place.Name,
                ["Description"] = place.Description ?? string.Empty,
                ["Address"] = place.Address ?? string.Empty
            };

            var translatedSets = await orchestrator.TranslateAsync(fields, "en", targetCodes, ct);
            var translated = translatedSets.FirstOrDefault();
            if (translated is null)
            {
                continue;
            }

            dbContext.PlaceTranslations.Add(PlaceTranslation.Create(
                place.Id,
                evt.LanguageId,
                translated.Fields["Name"],
                translated.Fields["Description"],
                translated.Fields["Address"]));
        }

        var businesses = await dbContext.Businesses
            .Include(b => b.BusinessTranslations)
            .ToListAsync(ct);

        foreach (var business in businesses)
        {
            if (business.BusinessTranslations.Any(t => t.LanguageId == evt.LanguageId))
            {
                continue;
            }

            var fields = new Dictionary<string, string>
            {
                ["Name"] = business.Name,
                ["Description"] = business.Description ?? string.Empty,
                ["Address"] = business.Address ?? string.Empty
            };

            var translatedSets = await orchestrator.TranslateAsync(fields, "en", targetCodes, ct);
            var translated = translatedSets.FirstOrDefault();
            if (translated is null)
            {
                continue;
            }

            dbContext.BusinessTranslations.Add(BusinessTranslation.Create(
                business.Id,
                evt.LanguageId,
                translated.Fields["Name"],
                translated.Fields["Description"],
                translated.Fields["Address"]));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
