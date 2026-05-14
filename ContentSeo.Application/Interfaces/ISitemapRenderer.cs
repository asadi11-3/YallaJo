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
}
