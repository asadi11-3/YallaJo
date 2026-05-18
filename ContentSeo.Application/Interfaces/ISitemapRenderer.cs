// <copyright file="ISitemapRenderer.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Renders the public XML sitemap and caches the result. Implementation is internal to ContentSeo.Infrastructure.
/// </summary>
public interface ISitemapRenderer
{
    /// <summary>Renders a fresh sitemap, persists any SitemapEntry mutations, and caches the XML.</summary>
    Task<string> RenderAndCacheAsync(CancellationToken ct);

    /// <summary>Returns the cached sitemap XML, rendering synchronously on cache miss.</summary>
    Task<string> GetCachedXmlAsync(CancellationToken ct);

    /// <summary>
    /// Returns the cached XML for a specific EntityType sub-sitemap (used when total URLs exceed 50,000).
    /// Returns null if the sub-sitemap does not exist or the total is below the sharding threshold.
    /// </summary>
    Task<string?> GetSubSitemapXmlAsync(string entityType, CancellationToken ct);
}
