using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

/// <summary>
/// Repository for the <see cref="SeoMetadata"/> aggregate root.
/// </summary>
public interface ISeoMetadataRepository : IRepository<SeoMetadata>
{
    /// <summary>
    /// Returns the SEO metadata for the given entity, or <c>null</c> if none exists.
    /// Used by the GET and Upsert handlers where the logical primary key is
    /// <c>(EntityType, EntityId)</c>, not the surrogate <c>Id</c>.
    /// </summary>
    Task<SeoMetadata?> GetByEntityAsync(
        SeoEntityType entityType,
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated slice of SEO metadata records ordered by most-recently updated.
    /// Used by the admin list endpoint. Soft-deleted records are excluded unless
    /// <paramref name="includeDeleted"/> is <c>true</c>.
    /// </summary>
    Task<(IReadOnlyList<SeoMetadata> Items, int Total)> ListAsync(
        SeoEntityType? entityType,
        bool includeDeleted,
        int skip,
        int take,
        CancellationToken ct = default);
}
