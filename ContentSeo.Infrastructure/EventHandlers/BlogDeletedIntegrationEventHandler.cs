using ContentBlogs.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Deactivates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Blog is deleted.
/// SEO records are never hard-deleted (audit trail + analytics history).
/// </summary>
public sealed class BlogDeletedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<BlogDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (BlogDeleted {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Blog" && s.EntityId == evt.BlogId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Blog {BlogId} not found on delete; nothing to deactivate.",
                evt.BlogId);
        }
        else if (sitemapEntry.IsActive)
        {
            sitemapEntry.Deactivate();
            logger.LogInformation(
                "ContentSeo: SitemapEntry deactivated for deleted Blog {BlogId}", evt.BlogId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
