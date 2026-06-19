using ContentCore.Contracts.Attachments;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Services;

internal sealed class PublicEntityImageReader(ContentCoreDbContext dbContext)
    : IPublicEntityImageReader
{
    public async Task<IReadOnlyList<EntityImageDto>> GetEntityImagesAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
        {
            return [];
        }

        if (!Enum.TryParse<EntityType>(entityType, ignoreCase: true, out var parsed))
        {
            return [];
        }

        return await dbContext.Set<EntityImage>()
            .AsNoTracking()
            .Where(img => img.EntityType == parsed
                       && img.EntityId == entityId
                       && img.Attachment.Type == AttachmentType.Image)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.SortOrder)
            .ThenBy(img => img.AttachmentId)
            .Select(img => new EntityImageDto(
                img.Attachment.Url,
                img.Attachment.ThumbnailUrl,
                img.SortOrder,
                img.IsPrimary))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<EntityImageDto>>> GetEntityImagesBatchAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType)
            || entityIds is null
            || entityIds.Count == 0
            || !Enum.TryParse<EntityType>(entityType, ignoreCase: true, out var parsed))
        {
            return new Dictionary<Guid, IReadOnlyList<EntityImageDto>>();
        }

        var rows = await dbContext.Set<EntityImage>()
            .AsNoTracking()
            .Where(img => img.EntityType == parsed
                       && entityIds.Contains(img.EntityId)
                       && img.Attachment.Type == AttachmentType.Image)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.SortOrder)
            .ThenBy(img => img.AttachmentId)
            .Select(img => new
            {
                img.EntityId,
                Dto = new EntityImageDto(
                    img.Attachment.Url,
                    img.Attachment.ThumbnailUrl,
                    img.SortOrder,
                    img.IsPrimary)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(row => row.EntityId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<EntityImageDto>)group.Select(row => row.Dto).ToList());
    }
}
