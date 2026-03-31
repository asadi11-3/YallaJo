using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository for <see cref="EntityTag"/> (entity–tag assignment).
///
/// INTENTIONAL EXCEPTION: EntityTag uses a composite primary key
/// (EntityType, EntityId, TagId) with no single Guid column.
/// It therefore cannot inherit from SharedKernel's generic IReadRepository or
/// IWriteRepository, which require a single-column key for GetByIdAsync /
/// DeleteByIdAsync support.
///
/// This interface is kept minimal — only the three operations that are actively
/// used by application handlers are declared here.
/// </summary>
public interface IEntityTagRepository
{
    /// <summary>
    /// Returns all tag assignments for the given entity, with the Tag
    /// navigation property eagerly loaded.
    /// </summary>
    Task<IReadOnlyList<EntityTag>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Stages a new entity–tag link for insertion.</summary>
    void Add(EntityTag entityTag);

    /// <summary>Stages an existing entity–tag link for deletion.</summary>
    void Remove(EntityTag entityTag);
}
