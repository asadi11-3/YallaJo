// <copyright file="SitemapEntryRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

internal sealed class SitemapEntryRepository(ContentSeoDbContext context)
    : EfRepository<SitemapEntry, Guid>(context), ISitemapEntryRepository
{
    public async Task<IReadOnlyList<SitemapEntry>> GetAllActiveAsync(CancellationToken ct = default)
        => await GetAllAsync(
            filter: s => s.IsActive && !s.IsDeleted,
            orderBy: q => q.OrderBy(s => s.Url),
            asNoTracking: true,
            ct: ct);

    public async Task<IReadOnlyList<SitemapEntry>> GetAllForRegenerationAsync(CancellationToken ct = default)
        => await GetAllAsync(
            filter: s => !s.IsDeleted,
            orderBy: q => q.OrderBy(s => s.Url),
            asNoTracking: false,
            ct: ct);

    public async Task<SitemapEntry?> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await GetAsync(s => s.EntityType == entityType && s.EntityId == entityId && !s.IsDeleted, asNoTracking: false, ct: ct);
}
