// <copyright file="ContentSeoCacheKeys.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Caching;

using ContentSeo.Domain.Enums;

/// <summary>
/// Cache keys and tags for the ContentSeo module.
/// </summary>
public static class ContentSeoCacheKeys
{
    // ---- Tag constants (shared) ----
    public const string TagRedirectsList = "redirects:list";
    public const string TagRedirectsLookup = "redirects:lookup";
    public const string TagSitemapRendered = "sitemap:rendered";

    // ---- Tag builders (per-entity / per-aggregate) ----
    public static string TagForSeo(SeoEntityType entityType, Guid entityId) =>
        $"seo:{entityType}:{entityId}";

    public static string TagForFaq(SeoEntityType entityType, Guid entityId) =>
        $"faq:{entityType}:{entityId}";

    public static string TagForSitemap(SeoEntityType entityType) =>
        $"sitemap:{entityType}";

    public static string TagForWeather(Guid placeId) =>
        $"weather:{placeId}";

    public static string TagForTranslations(string entityType, Guid entityId) =>
        $"translations:{entityType}:{entityId}";

    // ---- Cache key builders ----
    public static string SeoMetadata(SeoEntityType entityType, Guid entityId, string? acceptLanguage) =>
        $"seo:{entityType}:{entityId}:lang:{NormalizeLanguage(acceptLanguage)}";

    public static string RedirectsList(string filterHash, int page, int size) =>
        $"redirects:list:{filterHash}:{page}:{size}";

    public static string RedirectLookup(string oldUrl) =>
        $"redirects:lookup:{Hash(oldUrl)}";

    public static string FaqList(SeoEntityType entityType, Guid entityId, string? acceptLanguage) =>
        $"faq:{entityType}:{entityId}:lang:{NormalizeLanguage(acceptLanguage)}";

    // ---- Global FAQ list (F12.3) ----
    /// <summary>Coarse tag for invalidating ALL global FAQ list pages on any FAQ mutation.</summary>
    public const string TagAllFaqs = "faqs:list";

    public static string AllFaqs(int page, int pageSize, SeoEntityType? entityType, bool activeOnly, string? acceptLanguage) =>
        $"faqs:all:{page}:{pageSize}:{entityType?.ToString() ?? "any"}:{(activeOnly ? "active" : "any")}:lang:{NormalizeLanguage(acceptLanguage)}";

    public static string Sitemap() => "sitemap:rendered";

    public static string Weather(Guid placeId) => $"weather:{placeId}";

    /// <summary>
    /// PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
    /// Nearby tours share the same cache row.
    /// </summary>
    public static string WeatherByLocation(decimal roundedLat, decimal roundedLng, DateOnly date) =>
        $"weather:loc:{roundedLat:F2}:{roundedLng:F2}:{date:yyyy-MM-dd}";

    /// <summary>Tag for all weather cache entries at a given location.</summary>
    public static string TagForWeatherLocation(decimal roundedLat, decimal roundedLng) =>
        $"weather:loc:{roundedLat:F2}:{roundedLng:F2}";

    // ---- Helpers ----
    public static string Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "0";
        }

        unchecked
        {
            uint h = 2166136261u;
            foreach (var ch in input)
            {
                h ^= ch;
                h *= 16777619u;
            }
            return h.ToString("x");
        }
    }

    public static string NormalizeLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return "default";
        }

        var first = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var semi = first.IndexOf(';');
        if (semi >= 0)
        {
            first = first[..semi];
        }
        return first.Trim().ToLowerInvariant();
    }
}
