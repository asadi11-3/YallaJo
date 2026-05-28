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
/// When a creator profile is reinstated, batch un-hide all their hidden articles.
/// </summary>
public sealed class CreatorReinstatedUnhideArticlesHandler(
    ContentBlogsDbContext dbContext,
    IContentBlogsInboxStore inboxStore,
    IContentBlogsUnitOfWork unitOfWork,
    ILogger<CreatorReinstatedUnhideArticlesHandler> logger)
    : INotificationHandler<IntegrationEventNotification<CreatorProfileReinstatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<CreatorProfileReinstatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (CreatorProfileReinstated {ProfileId}) already processed; skipping.",
                notification.MessageId, notification.Event.ProfileId);
            return;
        }

        var evt = notification.Event;
        var utcNow = DateTime.UtcNow;

        // Load all hidden articles by this creator
        var hiddenArticles = await dbContext.Blogs
            .Where(b => b.AuthoredByCreatorId == evt.ProfileId
                        && b.Status == BlogStatus.Hidden
                        && !b.IsDeleted)
            .ToListAsync(ct);

        foreach (var article in hiddenArticles)
        {
            var unhideResult = article.Unhide(utcNow);
            if (unhideResult.IsFailure)
            {
                logger.LogWarning(
                    "ContentBlogs: Failed to unhide article {BlogId} for reinstated creator {ProfileId}: {Error}",
                    article.Id, evt.ProfileId, unhideResult.Errors.FirstOrDefault()?.Code);
            }
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentBlogs: Unhidden {Count} articles for reinstated creator ProfileId={ProfileId}",
            hiddenArticles.Count, evt.ProfileId);
    }
}
