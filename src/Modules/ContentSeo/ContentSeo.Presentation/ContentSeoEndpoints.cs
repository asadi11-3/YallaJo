// <copyright file="ContentSeoEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation;

using ContentSeo.Presentation.Endpoints.FaqItem;
using ContentSeo.Presentation.Endpoints.Redirect;
using ContentSeo.Presentation.Endpoints.SeoMetadata;
using ContentSeo.Presentation.Endpoints.Sitemap;
using ContentSeo.Presentation.Endpoints.Weather;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class ContentSeoEndpoints
{
    public static IEndpointRouteBuilder MapContentSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/seo");

        // Task 3 — SEO Metadata (3 endpoints)
        SeoMetadataEndpoints.MapSeoMetadataEndpoints(group.MapGroup("").WithTags("ContentSeo | SEO Metadata"));

        // Task 3 — Redirects (3 endpoints)
        RedirectEndpoints.MapRedirectEndpoints(group.MapGroup("").WithTags("ContentSeo | Redirects"));

        // Task 3 — FAQ Items (5 endpoints)
        FaqItemEndpoints.MapFaqItemEndpoints(group.MapGroup("").WithTags("ContentSeo | FAQ Items"));

        // Task 4 — Sitemap admin (1 endpoint: regenerate). Public /sitemap.xml is mapped at root.
        SitemapEndpoints.MapSitemapEndpoints(group.MapGroup("").WithTags("ContentSeo | Sitemap"));
        SitemapEndpoints.MapPublicSitemapEndpoint(endpoints.MapGroup("").WithTags("ContentSeo | Sitemap"));

        // Task 4 — Weather (2 endpoints)
        WeatherEndpoints.MapWeatherEndpoints(group.MapGroup("").WithTags("ContentSeo | Weather"));

        return endpoints;
    }
}
