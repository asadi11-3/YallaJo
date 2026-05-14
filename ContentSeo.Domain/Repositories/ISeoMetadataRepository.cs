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
}
