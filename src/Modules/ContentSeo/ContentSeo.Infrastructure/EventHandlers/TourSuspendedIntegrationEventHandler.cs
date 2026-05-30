using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Deactivates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Tour is suspended.
/// Suspended tours are hidden from public listing and should not appear in the sitemap.
/// </summary>
public sealed class TourSuspendedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourSuspendedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourSuspendedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (TourSuspended {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Tour" && s.EntityId == evt.TourId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Tour {TourId} not found on suspension; nothing to deactivate.",
                evt.TourId);
        }
        else if (sitemapEntry.IsActive)
        {
            sitemapEntry.Deactivate();
            logger.LogInformation(
                "ContentSeo: SitemapEntry deactivated for suspended Tour {TourId}", evt.TourId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
