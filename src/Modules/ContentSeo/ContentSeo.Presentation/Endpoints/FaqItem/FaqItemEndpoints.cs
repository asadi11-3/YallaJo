// <copyright file="FaqItemEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.FaqItem;

using ContentSeo.Application.Commands.FaqItem.CreateFaqItem;
using ContentSeo.Application.Commands.FaqItem.DeleteFaqItem;
using ContentSeo.Application.Commands.FaqItem.ReorderFaqItems;
using ContentSeo.Application.Commands.FaqItem.UpdateFaqItem;
using ContentSeo.Application.Queries.FaqItem.Common;
using ContentSeo.Application.Queries.FaqItem.GetAllFaqItems;
using ContentSeo.Application.Queries.FaqItem.GetFaqItems;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Domain.Enums;
using ContentSeo.Presentation.Endpoints.FaqItem.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class FaqItemEndpoints
{
    internal static void MapFaqItemEndpoints(RouteGroupBuilder group)
    {
        // GET /api/v1/seo/faq?page=1&pageSize=50&entityType=Tour&activeOnly=true
        // Closes F12.3 (previously 405 because only POST/PUT/DELETE matched /faq).
        group.MapGet("/faq", async (
            SeoEntityType? entityType,
            bool? activeOnly,
            int page,
            int pageSize,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var p = page <= 0 ? 1 : page;
            var ps = pageSize <= 0 ? 50 : pageSize;
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();
            var query = new GetAllFaqItemsQuery(p, ps, entityType, activeOnly ?? true, acceptLanguage);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("GetAllFaqItems")
        .WithSummary("List FAQ items across all entities (paginated). Active-only by default.")
        .Produces<PaginatedResult<FaqItemDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // GET /api/v1/seo/faq/{entityType}/{entityId}
        group.MapGet("/faq/{entityType}/{entityId:guid}", async (
            SeoEntityType entityType,
            Guid entityId,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();
            var query = new GetFaqItemsQuery(entityType, entityId, acceptLanguage);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("GetFaqItems")
        .WithSummary("List FAQ items for an entity.")
        .Produces<IReadOnlyList<FaqItemDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // POST /api/v1/seo/faq
        group.MapPost("/faq", async (
            CreateFaqItemRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateFaqItemCommand(
                request.EntityType,
                request.EntityId,
                request.Question,
                request.Answer,
                request.SortOrder,
                request.SourceLanguageCode);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/seo/faq/{request.EntityType}/{request.EntityId}");
        })
        .WithName("CreateFaqItem")
        .WithSummary("Create a new FAQ item.")
        .Produces<CreateFaqItemResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.FaqItem, AppAction.Create));

        // PUT /api/v1/seo/faq/{id}
        group.MapPut("/faq/{id:guid}", async (
            Guid id,
            UpdateFaqItemRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateFaqItemCommand(id, request.Question, request.Answer);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateFaqItem")
        .WithSummary("Update an FAQ item's question and answer.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.FaqItem, AppAction.Update));

        // DELETE /api/v1/seo/faq/{id}
        group.MapDelete("/faq/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new DeleteFaqItemCommand(id);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("DeleteFaqItem")
        .WithSummary("Soft-delete an FAQ item.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.FaqItem, AppAction.Delete));

        // PUT /api/v1/seo/faq/reorder
        group.MapPut("/faq/reorder", async (
            ReorderFaqItemsRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ReorderFaqItemsCommand(
                request.EntityType,
                request.EntityId,
                request.Items.Select(i => new ReorderFaqItemDto(i.Id, i.SortOrder)).ToList());
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ReorderFaqItems")
        .WithSummary("Reorder FAQ items in batch.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.FaqItem, AppAction.Update));
    }
}
