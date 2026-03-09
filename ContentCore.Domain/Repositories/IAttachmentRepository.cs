using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository for Attachment (non-aggregate entity).
/// Uses IReadRepository + IWriteRepository directly (not IRepository which requires IAggregateRoot).
/// Also manages EntityImage records for primary image selection.
/// </summary>
public interface IAttachmentRepository : IReadRepository<Attachment, Guid>, IWriteRepository<Attachment, Guid>
{
    Task<List<EntityImage>> GetEntityImagesAsync(EntityType entityType, Guid entityId, CancellationToken ct = default);
    void AddEntityImage(EntityImage entityImage);
    void UpdateEntityImage(EntityImage entityImage);
}