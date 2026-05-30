// <copyright file="SitemapRenderer.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Sitemap;

using System.Globalization;
using System.Text;
using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

internal sealed class SitemapRenderer(
    ISitemapEntryRepository sitemapEntryRepository,
    ISearchConsolePinger searchConsolePinger,
    HybridCache cache,
    ILogger<SitemapRenderer> logger) : ISitemapRenderer
{
    /// <summary>PDF §8: max 50,000 URLs per sitemap file; split into sitemap-index above this.</summary>
    private const int MaxEntries = 50_000;

    private const string SiteBaseUrl = "https://yallajo.com";
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(1),
    };

    public async Task<string> RenderAndCacheAsync(CancellationToken ct)
    {
        await Lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var entries = await sitemapEntryRepository.GetAllActiveAsync(ct).ConfigureAwait(false);

            string xml;

            if (entries.Count > MaxEntries)
            {
                // PDF §8: split into sitemap-index with per-EntityType sub-sitemaps.
                logger.LogInformation(
                    "Sitemap sharding: {Count} entries exceeds {Max}; generating sitemap-index.",
                    entries.Count, MaxEntries);

                xml = await BuildAndCacheSitemapIndexAsync(entries, ct).ConfigureAwait(false);
            }
            else
            {
                xml = BuildUrlSetXml(entries);
            }

            await cache.SetAsync(
                ContentSeoCacheKeys.Sitemap(),
                xml,
                Options,
                tags: [ContentSeoCacheKeys.TagSitemapRendered],
                cancellationToken: ct).ConfigureAwait(false);

            logger.LogInformation("Rendered sitemap with {Count} URLs", entries.Count);

            try
            {
                await searchConsolePinger.PingAsync($"{SiteBaseUrl}/sitemap.xml", ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Best effort, must not fail render
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "Search console ping failed (non-fatal).");
            }

            return xml;
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task<string> GetCachedXmlAsync(CancellationToken ct)
    {
        return await cache.GetOrCreateAsync<string>(
            ContentSeoCacheKeys.Sitemap(),
            async cancellation => await RenderAndCacheAsync(cancellation).ConfigureAwait(false),
            Options,
            tags: [ContentSeoCacheKeys.TagSitemapRendered],
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<string?> GetSubSitemapXmlAsync(string entityType, CancellationToken ct)
    {
        var cacheKey = $"sitemap:sub:{entityType.ToLowerInvariant()}";
        return await cache.GetOrCreateAsync<string?>(
            cacheKey,
            _ => ValueTask.FromResult<string?>(null),
            Options,
            tags: [ContentSeoCacheKeys.TagSitemapRendered],
            cancellationToken: ct).ConfigureAwait(false);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> BuildAndCacheSitemapIndexAsync(
        IReadOnlyList<SitemapEntry> entries,
        CancellationToken ct)
    {
        // Group by EntityType and cache each sub-sitemap.
        var groups = entries
            .GroupBy(e => e.EntityType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder(capacity: 2048);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var group in groups)
        {
            var entityType = group.Key;
            var subEntries = group.ToList();
            var subXml = BuildUrlSetXml(subEntries);

            // Cache the sub-sitemap.
            var subCacheKey = $"sitemap:sub:{entityType.ToLowerInvariant()}";
            await cache.SetAsync(
                subCacheKey,
                subXml,
                Options,
                tags: [ContentSeoCacheKeys.TagSitemapRendered],
                cancellationToken: ct).ConfigureAwait(false);

            var lastMod = subEntries
                .Select(e => e.LastModified ?? DateTime.UtcNow)
                .Max()
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            sb.Append("  <sitemap>");
            sb.Append("<loc>")
              .Append(EscapeXml($"{SiteBaseUrl}/sitemaps/{entityType.ToLowerInvariant()}.xml"))
              .Append("</loc>");
            sb.Append("<lastmod>").Append(lastMod).Append("</lastmod>");
            sb.AppendLine("</sitemap>");
        }

        sb.AppendLine("</sitemapindex>");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a standard urlset XML with hreflang ar + en + x-default per URL.
    /// PDF §8: hreflang ar AND en; canonical URLs always slug-based.
    /// </summary>
    private static string BuildUrlSetXml(IReadOnlyList<SitemapEntry> entries)
    {
        var sb = new StringBuilder(capacity: 4096);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine(
            "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" " +
            "xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

        foreach (var entry in entries)
        {
            var fullUrl = EscapeXml(SiteBaseUrl + entry.Url);
            var arUrl   = EscapeXml($"{SiteBaseUrl}/ar{entry.Url}");
            var enUrl   = EscapeXml($"{SiteBaseUrl}/en{entry.Url}");

            sb.Append("  <url>");
            sb.Append("<loc>").Append(fullUrl).Append("</loc>");

            var lastMod = entry.LastModified ?? DateTime.UtcNow;
            sb.Append("<lastmod>")
              .Append(lastMod.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
              .Append("</lastmod>");

            sb.Append("<changefreq>")
              .Append(EscapeXml(entry.ChangeFrequency ?? "weekly"))
              .Append("</changefreq>");

            sb.Append("<priority>")
              .Append((entry.Priority ?? 0.5m).ToString("F1", CultureInfo.InvariantCulture))
              .Append("</priority>");

            // PDF §8: hreflang ar AND en + x-default (canonical).
            sb.Append("<xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"").Append(arUrl).Append("\"/>");
            sb.Append("<xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"").Append(enUrl).Append("\"/>");
            sb.Append("<xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"").Append(fullUrl).Append("\"/>");

            sb.AppendLine("</url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private static string EscapeXml(string value)
        => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
}
