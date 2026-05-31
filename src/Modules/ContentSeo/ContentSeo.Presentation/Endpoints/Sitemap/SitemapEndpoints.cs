// <copyright file="SitemapEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Sitemap;

using ContentSeo.Application.Commands.Sitemap.RegenerateSitemap;
using ContentSeo.Application.Commands.Sitemap.DeleteSitemapEntry;
using ContentSeo.Application.Commands.Sitemap.UpdateSitemapEntry;
using ContentSeo.Application.Interfaces;
using ContentSeo.Application.Queries.Sitemap.Common;
using ContentSeo.Application.Queries.Sitemap.ListSitemapEntries;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Presentation.Endpoints.Sitemap.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class SitemapEndpoints
{
    internal static void MapSitemapEndpoints(RouteGroupBuilder group)
    {
        // GET /api/v1/seo/sitemap/entries (admin)
        group.MapGet("/sitemap/entries", async (
            string? entityType,
            bool? isActive,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 50 : pageSize;
            var result = await sender.Send(new ListSitemapEntriesQuery(entityType, isActive, p, ps), ct);
            return result.ToApiResult();
        })
        .WithName("ListSitemapEntries")
        .WithSummary("List sitemap entries with optional filters and pagination.")
        .Produces<PaginatedResult<SitemapEntryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Sitemap, AppAction.Read));

        // PATCH /api/v1/seo/sitemap/entries/{id}
        group.MapPatch("/sitemap/entries/{id:guid}", async (
            Guid id,
            UpdateSitemapEntryRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateSitemapEntryCommand(id, request.Priority, request.ChangeFrequency), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateSitemapEntry")
        .WithSummary("Override sitemap entry priority or change frequency.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Sitemap, AppAction.Update));

        // DELETE /api/v1/seo/sitemap/entries/{id}
        group.MapDelete("/sitemap/entries/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteSitemapEntryCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteSitemapEntry")
        .WithSummary("Delete a sitemap entry.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Sitemap, AppAction.Delete));

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
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .AllowAnonymous();

        // GET /sitemaps/{entityType}.xml — per-EntityType sub-sitemap (PDF §8 sharding)
        endpoints.MapGet("/sitemaps/{entityType}.xml", async (
            string entityType,
            ISitemapRenderer renderer,
            CancellationToken ct) =>
        {
            var xml = await renderer.GetSubSitemapXmlAsync(entityType, ct);
            if (xml is null)
                return Results.NotFound();
            return Results.Text(xml, "application/xml");
        })
        .WithName("GetSubSitemap")
        .WithSummary("Per-EntityType sub-sitemap (used when total URLs exceed 50,000).")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();
    }
}
