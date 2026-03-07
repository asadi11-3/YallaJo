using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Infrastructure.Persistence;
using ContentCore.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class LanguageActivatedIntegrationEventHandler(
    ContentBlogsDbContext dbContext,
    IEntityTranslationOrchestrator orchestrator,
    IContentBlogsUnitOfWork unitOfWork,
    IContentBlogsInboxStore inboxStore,
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
                "ContentBlogs: Message {MessageId} for language {LanguageId} already processed; skipping.",
                notification.MessageId,
                notification.Event.LanguageId);
            return;
        }

        var evt = notification.Event;
        var targetCodes = new List<string> { evt.LanguageCode };

        var blogs = await dbContext.Blogs
            .Include(b => b.BlogTranslations)
            .ToListAsync(ct);

        foreach (var blog in blogs)
        {
            if (blog.BlogTranslations.Any(t => t.LanguageId == evt.LanguageId))
            {
                continue;
            }

            var fields = new Dictionary<string, string>
            {
                ["Title"] = blog.Title,
                ["Content"] = blog.Content,
                ["Summary"] = blog.Summary ?? string.Empty
            };

            var translatedSets = await orchestrator.TranslateAsync(fields, "en", targetCodes, ct);
            var translated = translatedSets.FirstOrDefault();
            if (translated is null)
            {
                continue;
            }

            dbContext.BlogTranslations.Add(BlogTranslation.Create(
                blog.Id,
                evt.LanguageId,
                translated.Fields["Title"],
                translated.Fields["Content"],
                translated.Fields["Summary"]));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
