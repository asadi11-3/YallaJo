// <copyright file="SeoMetadataEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.SeoMetadata;

using ContentSeo.Application.Commands.SeoMetadata.DeleteSeoMetadata;
using ContentSeo.Application.Commands.SeoMetadata.UpdateSeoMetadata;
using ContentSeo.Application.Commands.SeoMetadata.UpsertSeoMetadata;
using ContentSeo.Application.Queries.SeoMetadata.Common;
using ContentSeo.Application.Queries.SeoMetadata.GetSeoMetadata;
using ContentSeo.Application.Queries.SeoMetadata.ListSeoMetadata;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Domain.Enums;
using ContentSeo.Presentation.Endpoints.SeoMetadata.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class SeoMetadataEndpoints
{
    public static void MapSeoMetadataEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/metadata/{entityType}/{entityId:guid}", async (
            SeoEntityType entityType,
            Guid entityId,
            ISender sender,
            HttpContext http,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();
            var query = new GetSeoMetadataQuery(entityType, entityId, acceptLanguage);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("GetSeoMetadata")
        .WithSummary("Get SEO metadata for a given entity.")
        .Produces<SeoMetadataDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapPost("/metadata", async (UpsertSeoMetadataRequest request, ISender sender, CancellationToken ct) =>
        {
            var cmd = new UpsertSeoMetadataCommand(
                request.EntityType,
                request.EntityId,
                request.MetaTitle,
                request.MetaDescription,
                request.OgTitle,
                request.OgDescription,
                request.OgImageUrl,
                request.SchemaMarkup,
                request.SitemapPriority,
                request.SitemapChangeFrequency,
                request.CanonicalUrl);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/seo/metadata/{request.EntityType}/{request.EntityId}");
        })
        .WithName("UpsertSeoMetadata")
        .WithSummary("Create or update SEO metadata for an entity.")
        .Produces<UpsertSeoMetadataResult>(StatusCodes.Status200OK)
        .Produces<UpsertSeoMetadataResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.SeoMetadata, AppAction.Create));

        group.MapPut("/metadata/{id:guid}", async (Guid id, UpdateSeoMetadataRequest request, ISender sender, CancellationToken ct) =>
        {
            var cmd = new UpdateSeoMetadataCommand(
                id,
                request.MetaTitle,
                request.MetaDescription,
                request.OgTitle,
                request.OgDescription,
                request.OgImageUrl,
                request.SchemaMarkup,
                request.SitemapPriority,
                request.SitemapChangeFrequency,
                request.CanonicalUrl);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateSeoMetadata")
        .WithSummary("Update SEO metadata by id.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.SeoMetadata, AppAction.Update));

        group.MapDelete("/metadata/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteSeoMetadataCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteSeoMetadata")
        .WithSummary("Soft-delete SEO metadata by id.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.SeoMetadata, AppAction.Delete));

        group.MapGet("/metadata", async (
            SeoEntityType? entityType,
            bool? includeDeleted,
            int? skip,
            int? take,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListSeoMetadataQuery(
                entityType,
                includeDeleted ?? false,
                skip ?? 0,
                take ?? 50);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("ListSeoMetadata")
        .WithSummary("Admin list of SEO metadata, optionally filtered by entity type, with paging.")
        .Produces<ListSeoMetadataResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.SeoMetadata, AppAction.Read));
    }
}
