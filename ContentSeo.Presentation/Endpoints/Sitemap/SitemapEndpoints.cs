// <copyright file="SitemapEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Sitemap;

using ContentSeo.Application.Commands.Sitemap.RegenerateSitemap;
using ContentSeo.Application.Interfaces;
using ContentSeo.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class SitemapEndpoints
{
    internal static void MapSitemapEndpoints(RouteGroupBuilder group)
    {
        // POST /api/v1/seo/sitemap/regenerate (admin)
        group.MapPost("/sitemap/regenerate", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegenerateSitemapCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("RegenerateSitemap")
        .WithSummary("Force a sitemap re-render and cache refresh.")
        .Produces<RegenerateSitemapResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Sitemap, AppAction.Refresh));
    }

    internal static void MapPublicSitemapEndpoint(IEndpointRouteBuilder endpoints)
    {
        // GET /sitemap.xml (anonymous, top-level)
        endpoints.MapGet("/sitemap.xml", async (
            ISitemapRenderer renderer,
            CancellationToken ct) =>
        {
            var xml = await renderer.GetCachedXmlAsync(ct);
            return Results.Text(xml, "application/xml");
        })
        .WithName("GetSitemap")
        .WithSummary("Public XML sitemap (used by search engines).")
        .WithTags("ContentSeo")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .AllowAnonymous();
    }
}
