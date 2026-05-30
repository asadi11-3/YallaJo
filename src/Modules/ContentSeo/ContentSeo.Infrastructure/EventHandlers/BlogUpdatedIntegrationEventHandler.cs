using ContentBlogs.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Updates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> URL when a Blog's slug changes.
/// PDF §8: slug change auto-creates 301 redirect (handled by ContentBlogs); ContentSeo updates sitemap URL.
/// </summary>
public sealed class BlogUpdatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<BlogUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogUpdatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogUpdatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (BlogUpdated {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;

        // Only update sitemap URL if the slug changed.
        if (evt.OldSlug != evt.NewSlug)
        {
            var sitemapEntry = await dbContext.SitemapEntries
                .FirstOrDefaultAsync(s => s.EntityType == "Blog" && s.EntityId == evt.BlogId, ct);

            if (sitemapEntry is not null)
            {
                sitemapEntry.ChangeUrl($"/blog/{evt.NewSlug}");
                sitemapEntry.UpdateLastModified(evt.UpdatedAt);
                logger.LogInformation(
                    "ContentSeo: SitemapEntry URL updated for Blog {BlogId}: /blog/{OldSlug} → /blog/{NewSlug}",
                    evt.BlogId, evt.OldSlug, evt.NewSlug);
            }
            else
            {
                logger.LogWarning(
                    "ContentSeo: SitemapEntry for Blog {BlogId} not found on slug change; skipping URL update.",
                    evt.BlogId);
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
