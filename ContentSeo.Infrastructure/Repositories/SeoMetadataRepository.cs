using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISeoMetadataRepository"/>.
/// </summary>
internal sealed class SeoMetadataRepository(ContentSeoDbContext context)
    : EfRepository<SeoMetadata, Guid>(context), ISeoMetadataRepository
{
    /// <inheritdoc/>
    public Task<SeoMetadata?> GetByEntityAsync(
        SeoEntityType entityType,
        Guid entityId,
        CancellationToken ct = default) =>
        GetAsync(
            filter: m => m.EntityType == entityType && m.EntityId == entityId,
            ct: ct);
}
