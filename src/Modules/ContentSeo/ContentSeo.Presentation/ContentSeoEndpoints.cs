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
        var group = endpoints.MapGroup("/api/v1/seo")
            .WithTags("ContentSeo");

        // Task 3 — SEO Metadata (3 endpoints)
        SeoMetadataEndpoints.MapSeoMetadataEndpoints(group);

        // Task 3 — Redirects (3 endpoints)
        RedirectEndpoints.MapRedirectEndpoints(group);

        // Task 3 — FAQ Items (5 endpoints)
        FaqItemEndpoints.MapFaqItemEndpoints(group);

        // Task 4 — Sitemap admin (1 endpoint: regenerate). Public /sitemap.xml is mapped at root.
        SitemapEndpoints.MapSitemapEndpoints(group);
        SitemapEndpoints.MapPublicSitemapEndpoint(endpoints);

        // Task 4 — Weather (2 endpoints)
        WeatherEndpoints.MapWeatherEndpoints(group);

        return endpoints;
    }
}
