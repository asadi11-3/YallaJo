// <copyright file="ISitemapEntryRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

public interface ISitemapEntryRepository : IRepository<SitemapEntry>
{
    /// <summary>Returns all active sitemap entries (untracked) ordered by url.</summary>
    Task<IReadOnlyList<SitemapEntry>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Returns all sitemap entries (tracked) for sitemap regeneration.</summary>
    Task<IReadOnlyList<SitemapEntry>> GetAllForRegenerationAsync(CancellationToken ct = default);

    /// <summary>Looks up a sitemap entry by entity coordinates (tracked).</summary>
    Task<SitemapEntry?> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
