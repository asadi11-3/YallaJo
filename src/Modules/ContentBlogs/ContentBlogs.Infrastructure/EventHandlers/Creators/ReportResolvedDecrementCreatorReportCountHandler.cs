using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Consumes <c>social.report.resolved.v1</c>. If the report was resolved with <c>Dismiss</c>
/// (spam / unfounded) against creator content, atomically decrements <c>CreatorProfile.ReportCount</c>.
/// Only decrements for dismissed reports — legitimate removals keep the count.
/// </summary>
public sealed class ReportResolvedDecrementCreatorReportCountHandler(
    ContentBlogsDbContext dbContext,
    IContentBlogsInboxStore inboxStore,
    IContentBlogsUnitOfWork unitOfWork,
    ICreatorProfileRepository profileRepository,
    ILogger<ReportResolvedDecrementCreatorReportCountHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReportResolvedIntegrationEvent>>
{
    private const string EntityTypeBlog = "Blog";
    private const string ActionDismiss = "Dismiss";

    public async Task Handle(
        IntegrationEventNotification<ReportResolvedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (ReportResolved) already processed; skipping.",
                notification.MessageId);
            return;
        }

        var evt = notification.Event;

        // Only decrement if the report was dismissed (spam / unfounded).
        // Legitimate moderation actions keep the report count intact.
        if (!string.Equals(evt.Action, ActionDismiss, StringComparison.OrdinalIgnoreCase))
        {
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogDebug(
                "ContentBlogs: Report {ReportId} resolved with Action={Action} (not Dismiss); skipping decrement.",
                evt.ReportId, evt.Action);
            return;
        }

        // Resolve the creator profile ID from the reported entity (Blog only — CreatorPost merged into Blog)
        Guid? creatorProfileId = evt.EntityType switch
        {
            EntityTypeBlog => await dbContext.Blogs
                .Where(b => b.Id == evt.EntityId && !b.IsDeleted && b.AuthoredByCreatorId != null)
                .Select(b => b.AuthoredByCreatorId)
                .FirstOrDefaultAsync(ct),

            _ => null
        };

        if (creatorProfileId is null)
        {
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // Mark inbox FIRST to prevent duplicate processing if counter fails
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        // Now safely decrement counter (auto-commits, idempotent inbox prevents re-decrement)
        var rows = await profileRepository.AtomicDecrementReportCountAsync(creatorProfileId.Value, ct);
        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: CreatorProfile {ProfileId} not found when decrementing ReportCount for Report {ReportId}.",
                creatorProfileId, evt.ReportId);
        }

        logger.LogInformation(
            "ContentBlogs: Decremented ReportCount for CreatorProfile {ProfileId} — dismissed Report {ReportId} on {EntityType} {EntityId}.",
            creatorProfileId, evt.ReportId, evt.EntityType, evt.EntityId);
    }
}
