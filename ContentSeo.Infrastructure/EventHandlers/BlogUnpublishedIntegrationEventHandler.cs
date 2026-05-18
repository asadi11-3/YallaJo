using ContentBlogs.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Deactivates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Blog is unpublished.
/// Unpublished blogs revert to Draft — not visible in sitemap.
/// </summary>
public sealed class BlogUnpublishedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<BlogUnpublishedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogUnpublishedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogUnpublishedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (BlogUnpublished {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Blog" && s.EntityId == evt.BlogId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Blog {BlogId} not found on unpublish; nothing to deactivate.",
                evt.BlogId);
        }
        else if (sitemapEntry.IsActive)
        {
            sitemapEntry.Deactivate();
            logger.LogInformation(
                "ContentSeo: SitemapEntry deactivated for unpublished Blog {BlogId}", evt.BlogId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
