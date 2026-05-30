using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class CreatorDeactivatedSeoHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<CreatorDeactivatedSeoHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileDeactivatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<CreatorProfileDeactivatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;
        var sitemapEntry = await dbContext.SitemapEntries.FirstOrDefaultAsync(s => s.EntityType == "Creator" && s.EntityId == evt.ProfileId, ct);
        if (sitemapEntry is { IsActive: true }) sitemapEntry.Deactivate();
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("ContentSeo: sitemap deactivated for Creator {ProfileId}", evt.ProfileId);
    }
}
