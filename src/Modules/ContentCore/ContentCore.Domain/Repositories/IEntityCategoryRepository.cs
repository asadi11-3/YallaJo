using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository for <see cref="EntityCategory"/> (entity–category assignment).
///
/// INTENTIONAL EXCEPTION: EntityCategory uses a composite primary key
/// (EntityType, EntityId, CategoryId) with no single Guid column.
/// It therefore cannot inherit from SharedKernel's generic IReadRepository or
/// IWriteRepository, which require a single-column key for GetByIdAsync /
/// DeleteByIdAsync support.
///
/// This interface is kept minimal — only the three operations that are actively
/// used by application handlers are declared here.
/// </summary>
public interface IEntityCategoryRepository 
{
    /// <summary>
    /// Returns all category assignments for the given entity, with the Category
    /// navigation property eagerly loaded.
    /// </summary>
    Task<IReadOnlyList<EntityCategory>> GetByEntityAsync(
        EntityType entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Stages a new entity–category link for insertion.</summary>
    void Add(EntityCategory entityCategory);

    /// <summary>Stages an existing entity–category link for deletion.</summary>
    void Remove(EntityCategory entityCategory);
}
