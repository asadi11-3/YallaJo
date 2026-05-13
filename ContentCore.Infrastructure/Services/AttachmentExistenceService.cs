using ContentCore.Contracts.Attachments;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Services;

internal sealed class AttachmentExistenceService(ContentCoreDbContext dbContext)
    : IAttachmentExistenceService
{
    public async Task<bool> HasEntityImageAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
        {
            return false;
        }

        if (!Enum.TryParse<EntityType>(entityType, ignoreCase: true, out var parsed))
        {
            return false;
        }

        return await dbContext.Set<EntityImage>()
            .AsNoTracking()
            .AnyAsync(
                x => x.EntityType == parsed && x.EntityId == entityId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
