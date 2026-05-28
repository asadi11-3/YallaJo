using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class CreatorProfileUpdatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<CreatorProfileUpdatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileUpdatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<CreatorProfileUpdatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "Creator" && s.EntityId == evt.ProfileId, ct);
        if (sitemapEntry is not null)
        {
            if (!string.Equals(evt.OldSlug, evt.NewSlug, StringComparison.Ordinal)) sitemapEntry.ChangeUrl($"/creators/{evt.NewSlug}");
            sitemapEntry.UpdateLastModified(evt.UpdatedAtUtc);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap updated for Creator {ProfileId}", evt.ProfileId);
    }
}
