using ContentBlogs.Application.Interfaces;
using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// When a creator profile is suspended, batch-hide all their published articles.
/// </summary>
public sealed class CreatorSuspendedHideArticlesHandler(
    ContentBlogsDbContext dbContext,
    IContentBlogsInboxStore inboxStore,
    IContentBlogsUnitOfWork unitOfWork,
    ILogger<CreatorSuspendedHideArticlesHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileSuspendedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CreatorProfileSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (CreatorProfileSuspended {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.ProfileId);
            return;
        }

        var evt = notification.Event;
        var utcNow = DateTime.UtcNow;

        // Load all published articles by this creator
        var publishedArticles = await dbContext.Blogs
            .Where(b => b.AuthoredByCreatorId == evt.ProfileId
                        && b.Status == BlogStatus.Published
                        && !b.IsDeleted)
            .ToListAsync(ct);

        foreach (var article in publishedArticles)
        {
            var hideResult = article.Hide(evt.SuspendedByAdminId, evt.Reason, utcNow);
            if (hideResult.IsFailure)
            {
                logger.LogWarning(
                    "ContentBlogs: Failed to hide article {BlogId} for suspended creator {ProfileId}: {Error}",
                    article.Id, evt.ProfileId, hideResult.Errors.FirstOrDefault()?.Code);
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentBlogs: Hidden {Count} published articles for suspended creator ProfileId={ProfileId}",
            publishedArticles.Count, evt.ProfileId);
    }
}
