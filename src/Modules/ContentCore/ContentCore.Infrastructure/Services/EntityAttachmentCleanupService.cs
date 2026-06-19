using ContentCore.Contracts.Attachments;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

namespace ContentCore.Infrastructure.Services;

internal sealed class EntityAttachmentCleanupService(
    ContentCoreDbContext dbContext,
    IFileStorageService fileStorageService,
    ILogger<EntityAttachmentCleanupService> logger)
    : IEntityAttachmentCleanupService
{
    public async Task<int> DeleteEntityAttachmentsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType)
            || entityId == Guid.Empty
            || !Enum.TryParse<EntityType>(entityType, ignoreCase: true, out var parsed))
        {
            return 0;
        }

        var attachments = await dbContext.Set<Attachment>()
            .Where(a => a.EntityType == parsed && a.EntityId == entityId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (attachments.Count == 0)
        {
            return 0;
        }

        // Capture URLs before removing rows so we can clean up physical files after commit.
        var fileUrls = new List<string>(attachments.Count * 2);
        foreach (var attachment in attachments)
        {
            if (!string.IsNullOrWhiteSpace(attachment.Url))
            {
                fileUrls.Add(attachment.Url);
            }

            if (!string.IsNullOrWhiteSpace(attachment.ThumbnailUrl))
            {
                fileUrls.Add(attachment.ThumbnailUrl);
            }
        }

        var entityImages = await dbContext.Set<EntityImage>()
            .Where(img => img.EntityType == parsed && img.EntityId == entityId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entityImages.Count > 0)
        {
            dbContext.Set<EntityImage>().RemoveRange(entityImages);
        }

        dbContext.Set<Attachment>().RemoveRange(attachments);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Best-effort physical file deletion: never fail the caller's lifecycle operation.
        foreach (var fileUrl in fileUrls)
        {
            try
            {
                await fileStorageService.DeleteAsync(fileUrl, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to delete attachment file for {EntityType}/{EntityId}; an orphaned file can be cleaned up manually.",
                    entityType,
                    entityId);
            }
        }

        return attachments.Count;
    }
}
