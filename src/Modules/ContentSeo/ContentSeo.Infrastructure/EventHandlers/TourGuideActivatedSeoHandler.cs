using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class TourGuideActivatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourGuideActivatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourGuideActivatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<TourGuideActivatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        if (!await dbContext.SeoMetadata.AnyAsync(s => s.EntityType == SeoEntityType.TourGuide && s.EntityId == evt.TourGuideId, ct))
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                SeoEntityType.TourGuide,
                evt.TourGuideId,
                $"{evt.DisplayName} | Tour Guide",
                sitemapPriority: 0.7m,
                sitemapChangeFrequency: "weekly"));
        }

        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "TourGuide" && s.EntityId == evt.TourGuideId, ct);
        if (sitemapEntry is null)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create($"/guides/{evt.Slug}", "TourGuide", evt.TourGuideId, "weekly", 0.7m, true, evt.ActivatedAtUtc));
        }
        else if (!sitemapEntry.IsActive)
        {
            sitemapEntry.Reactivate();
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap activated for TourGuide {TourGuideId}", evt.TourGuideId);
    }
}
