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
/// Consumes <c>social.report.submitted.v1</c>. If the reported entity is a
/// <c>Blog</c> authored by a creator, atomically increments <c>CreatorProfile.ReportCount</c>.
/// </summary>
public sealed class ReportCreatedIncrementCreatorReportCountHandler(
    ContentBlogsDbContext dbContext,
    IContentBlogsInboxStore inboxStore,
    IContentBlogsUnitOfWork unitOfWork,
    ICreatorProfileRepository profileRepository,
    ILogger<ReportCreatedIncrementCreatorReportCountHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReportSubmittedIntegrationEvent>>
{
    private const string EntityTypeBlog = "Blog";

    public async Task Handle(
        IntegrationEventNotification<ReportSubmittedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (ReportSubmitted) already processed; skipping.",
                notification.MessageId);
            return;
        }

        var evt = notification.Event;

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
            // Not a creator-authored entity — nothing to do
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // Mark inbox FIRST to prevent duplicate processing if counter fails
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        // Now safely increment counter (auto-commits, idempotent inbox prevents re-increment)
        var rows = await profileRepository.AtomicIncrementReportCountAsync(creatorProfileId.Value, ct);
        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: CreatorProfile {ProfileId} not found when incrementing ReportCount for Report {ReportId}.",
                creatorProfileId, evt.ReportId);
        }

        logger.LogInformation(
            "ContentBlogs: Incremented ReportCount for CreatorProfile {ProfileId} due to Report {ReportId} on {EntityType} {EntityId}.",
            creatorProfileId, evt.ReportId, evt.EntityType, evt.EntityId);
    }
}
