using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class TourGuideDeactivatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourGuideDeactivatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourGuideDeactivatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourGuideDeactivatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "TourGuide" && s.EntityId == evt.TourGuideId, ct);
        if (sitemapEntry is { IsActive: true }) sitemapEntry.Deactivate();
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap deactivated for TourGuide {TourGuideId}", evt.TourGuideId);
    }
}
