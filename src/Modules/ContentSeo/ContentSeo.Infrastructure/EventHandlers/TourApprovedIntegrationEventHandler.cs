using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Activates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Tour is approved.
/// Approved tours are publicly visible and should appear in the sitemap.
/// </summary>
public sealed class TourApprovedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourApprovedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (TourApproved {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Tour" && s.EntityId == evt.TourId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Tour {TourId} not found on approval; nothing to activate.",
                evt.TourId);
        }
        else if (!sitemapEntry.IsActive)
        {
            sitemapEntry.Reactivate();
            sitemapEntry.UpdateLastModified(evt.ApprovedAt);
            logger.LogInformation(
                "ContentSeo: SitemapEntry activated for approved Tour {TourId}", evt.TourId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
