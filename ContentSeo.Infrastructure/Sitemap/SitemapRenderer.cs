// <copyright file="SitemapRenderer.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Sitemap;

using System.Globalization;
using System.Text;
using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

internal sealed class SitemapRenderer(
    ISitemapEntryRepository sitemapEntryRepository,
    ISearchConsolePinger searchConsolePinger,
    HybridCache cache,
    ILogger<SitemapRenderer> logger) : ISitemapRenderer
{
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

            if (entries.Count > MaxEntries)
            {
                logger.LogError("Sitemap rendering aborted: {Count} entries exceeds {Max} limit.", entries.Count, MaxEntries);
                throw new InvalidOperationException($"Sitemap.SizeOverflow: {entries.Count} entries exceeds {MaxEntries}.");
            }

            var xml = BuildXml(entries);

            await cache.SetAsync(
                ContentSeoCacheKeys.Sitemap(),
                xml,
                Options,
                tags: new[] { ContentSeoCacheKeys.TagSitemapRendered },
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
            tags: new[] { ContentSeoCacheKeys.TagSitemapRendered },
            cancellationToken: ct).ConfigureAwait(false);
    }

    private static string BuildXml(IReadOnlyList<SitemapEntry> entries)
    {
        var sb = new StringBuilder(capacity: 4096);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

        foreach (var entry in entries)
        {
            sb.Append("  <url>");
            sb.Append("<loc>").Append(EscapeXml(SiteBaseUrl + entry.Url)).Append("</loc>");
            var lastMod = entry.LastModified ?? DateTime.UtcNow;
            sb.Append("<lastmod>").Append(lastMod.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).Append("</lastmod>");
            sb.Append("<changefreq>").Append(EscapeXml(entry.ChangeFrequency ?? "weekly")).Append("</changefreq>");
            sb.Append("<priority>").Append((entry.Priority ?? 0.5m).ToString("F1", CultureInfo.InvariantCulture)).Append("</priority>");
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
