using ContentCore.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class LanguageActivatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
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
                "ContentSeo: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var targetCodes = new List<string> { evt.LanguageCode };

        var faqItems = await dbContext.FaqItems
            .Include(f => f.FaqItemTranslations)
            .ToListAsync(ct);

        foreach (var faqItem in faqItems)
        {
            if (faqItem.FaqItemTranslations.Any(t => t.LanguageId == evt.LanguageId))
            {
                continue;
            }

            var fields = new Dictionary<string, string>
            {
                ["Question"] = faqItem.Question,
                ["Answer"] = faqItem.Answer
            };

            var translatedSets = await orchestrator.TranslateAsync(fields, "en", targetCodes, ct);
            var translated = translatedSets.FirstOrDefault();
            if (translated is null)
            {
                continue;
            }

            dbContext.FaqItemTranslations.Add(FaqItemTranslation.Create(
                faqItem.Id,
                evt.LanguageId,
                translated.Fields["Question"],
                translated.Fields["Answer"]));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
