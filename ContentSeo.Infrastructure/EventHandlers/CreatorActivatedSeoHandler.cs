using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class CreatorActivatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<CreatorActivatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileActivatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<CreatorProfileActivatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        if (!await dbContext.SeoMetadata.AnyAsync(s => s.EntityType == SeoEntityType.Creator && s.EntityId == evt.ProfileId, ct))
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                SeoEntityType.Creator,
                evt.ProfileId,
                $"{evt.DisplayName} | Creator",
                sitemapPriority: 0.6m,
                sitemapChangeFrequency: "weekly"));
        }

        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "Creator" && s.EntityId == evt.ProfileId, ct);
        if (sitemapEntry is null)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create($"/creators/{evt.Slug}", "Creator", evt.ProfileId, "weekly", 0.6m, true, evt.ActivatedAtUtc));
        }
        else if (!sitemapEntry.IsActive)
        {
            sitemapEntry.Reactivate();
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap activated for Creator {ProfileId}", evt.ProfileId);
    }
}
