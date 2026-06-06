using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<SeoMetadata> Items, int Total)> ListAsync(
        SeoEntityType? entityType,
        bool includeDeleted,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = context.SeoMetadata.AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(m => !m.IsDeleted);
        }

        if (entityType.HasValue)
        {
            var et = entityType.Value;
            query = query.Where(m => m.EntityType == et);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }
}
