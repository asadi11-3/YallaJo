using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class TourGuideProfileUpdatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourGuideProfileUpdatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourGuideProfileUpdatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourGuideProfileUpdatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "TourGuide" && s.EntityId == evt.TourGuideId, ct);
        if (sitemapEntry is not null)
        {
            if (!string.Equals(evt.OldSlug, evt.NewSlug, StringComparison.Ordinal)) sitemapEntry.ChangeUrl($"/guides/{evt.NewSlug}");
            sitemapEntry.UpdateLastModified(evt.UpdatedAtUtc);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap updated for TourGuide {TourGuideId}", evt.TourGuideId);
    }
}
