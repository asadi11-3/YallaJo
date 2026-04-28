using ContentCore.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class LanguageActivatedIntegrationEventHandler(
    ContentToursDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentToursUnitOfWork unitOfWork,
    IContentToursInboxStore inboxStore,
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
                "ContentTours: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var normalizedLanguageCode = evt.LanguageCode.Trim().ToLowerInvariant();
        var targetCodes = new List<string> { normalizedLanguageCode };

        var tours = await dbContext.Tours
            .Include(t => t.TourTranslations)
            .Include(t => t.TourPricingTiers)
                .ThenInclude(t => t.Translations)
            .ToListAsync(ct);

        foreach (var tour in tours)
        {
            if (!tour.TourTranslations.Any(t => t.LanguageId == evt.LanguageId))
            {
                var fields = new Dictionary<string, string>
                {
                    ["Name"] = tour.Name,
                    ["Description"] = tour.Description ?? string.Empty,
                    ["ShortDescription"] = tour.ShortDescription ?? string.Empty,
                    ["MeetingPoint"] = tour.MeetingPoint?.ToString() ?? string.Empty
                };

                var translatedSets = await orchestrator.TranslateAsync(fields, "en", targetCodes, ct);
                var translated = translatedSets.FirstOrDefault();
                if (translated is not null)
                {
                    dbContext.TourTranslations.Add(TourTranslation.Create(
                        tour.Id,
                        evt.LanguageId,
                        translated.Fields["Name"],
                        translated.Fields["Description"],
                        translated.Fields["ShortDescription"],
                        translated.Fields["MeetingPoint"]));
                }
            }

            foreach (var tier in tour.TourPricingTiers)
            {
                if (tier.Translations.Any(t => t.LanguageCode == normalizedLanguageCode))
                {
                    continue;
                }

                dbContext.TourPricingTierTranslations.Add(TourPricingTierTranslation.Create(
                    tier.Id,
                    normalizedLanguageCode,
                    tier.Name,
                    tier.Description));
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentTours: Backfilled tours and pricing tiers for {TourCount} tours to language {LanguageCode}.",
            tours.Count, normalizedLanguageCode);
    }
}
