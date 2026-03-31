using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class AttachmentRepository(ContentCoreDbContext context)
    : EfEntityRepository<Attachment, Guid>(context), IAttachmentRepository
{
    public async Task<IReadOnlyList<EntityImage>> GetEntityImagesAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default)
        => await _context.Set<EntityImage>()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .ToListAsync(ct);

    public void AddEntityImage(EntityImage entityImage)
        => _context.Set<EntityImage>().Add(entityImage);

    public void UpdateEntityImage(EntityImage entityImage)
        => _context.Set<EntityImage>().Update(entityImage);
}
