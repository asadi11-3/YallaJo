using ContentBlogs.Application.Commands.Creator.Posts.CreatePost;
using ContentBlogs.Application.Commands.Creator.Posts.DeletePost;
using ContentBlogs.Application.Commands.Creator.Posts.PublishPost;
using ContentBlogs.Application.Commands.Creator.Posts.SubmitPostForReview;
using ContentBlogs.Application.Commands.Creator.Posts.UpdatePost;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Application.Queries.Creator.Posts.GetCreatorPostBySlug;
using ContentBlogs.Application.Queries.Creator.Posts.GetFeaturedPosts;
using ContentBlogs.Application.Queries.Creator.Posts.GetMyCreatorPosts;
using ContentBlogs.Application.Queries.Creator.Posts.ListPosts;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Presentation.Endpoints.Creator.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentBlogs.Presentation.Endpoints.Creator;

internal static class CreatorPostEndpoints
{
    internal static void MapCreatorPostEndpoints(RouteGroupBuilder blogGroup)
    {
        var group = blogGroup.MapGroup("/creators/posts")
            .WithTags("Creator Posts");

        // ══════════════════════════════════════════════════════════════════
        //  PUBLIC / ANONYMOUS ENDPOINTS
        // ══════════════════════════════════════════════════════════════════

        // ── GET /api/v1/blogs/creators/posts ─────────────────────────────
        group.MapGet("/", async (
            Guid? creatorProfileId,
            CreatorPostType? type,
            Guid? nicheId,
            string? search,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new ListCreatorPostsQuery(
                creatorProfileId, type, nicheId, search,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("ListCreatorPosts")
        .WithSummary("List published creator posts with optional filters")
        .Produces<PaginatedResult<CreatorPostSummaryDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // ── GET /api/v1/blogs/creators/posts/featured ────────────────────
        group.MapGet("/featured", async (
            int? limit,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetFeaturedCreatorPostsQuery(limit ?? 12), ct);
            return result.ToApiResult();
        })
        .WithName("GetFeaturedCreatorPosts")
        .WithSummary("Get currently featured creator posts")
        .Produces<IReadOnlyList<CreatorPostSummaryDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // ── GET /api/v1/blogs/creators/posts/{slug} ──────────────────────
        group.MapGet("/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCreatorPostBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetCreatorPostBySlug")
        .WithSummary("Get a published creator post by slug")
        .Produces<CreatorPostDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ══════════════════════════════════════════════════════════════════
        //  CREATOR SELF-SERVICE ENDPOINTS
        // ══════════════════════════════════════════════════════════════════

        // ── GET /api/v1/blogs/creators/posts/mine ────────────────────────
        group.MapGet("/mine", async (
            CreatorPostStatus? status,
            CreatorPostType? type,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            // CreatorProfileId is resolved from the authenticated user inside the handler
            var query = new GetMyCreatorPostsQuery(
                status, type,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("GetMyCreatorPosts")
        .WithSummary("List current creator's own posts with status/type filter")
        .Produces<PaginatedResult<CreatorPostSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Read));

        // ── POST /api/v1/blogs/creators/posts ────────────────────────────
        group.MapPost("/", async (
            CreateCreatorPostRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateCreatorPostCommand(
                PostType: request.PostType,
                Title: request.Title,
                Excerpt: request.Excerpt,
                LanguageId: request.LanguageId,
                TypeSpecificDataJson: request.TypeSpecificDataJson,
                NicheIds: request.NicheIds,
                FreeTags: request.FreeTags);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/blogs/creators/posts/{r.Slug}");
        })
        .WithName("CreateCreatorPost")
        .WithSummary("Create a new creator post (Draft)")
        .Produces<CreateCreatorPostResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Create));

        // ── PUT /api/v1/blogs/creators/posts/{postId} ────────────────────
        group.MapPut("/{postId:guid}", async (
            Guid postId,
            UpdateCreatorPostRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateCreatorPostCommand(
                PostId: postId,
                Title: request.Title,
                Excerpt: request.Excerpt,
                Body: request.Body,
                TypeSpecificDataJson: request.TypeSpecificDataJson);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCreatorPost")
        .WithSummary("Update a Draft creator post")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Update));

        // ── POST /api/v1/blogs/creators/posts/{postId}/submit ────────────
        group.MapPost("/{postId:guid}/submit", async (
            Guid postId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SubmitCreatorPostForReviewCommand(postId), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitCreatorPostForReview")
        .WithSummary("Submit a Draft post for admin review (Tier 0)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Submit));

        // ── POST /api/v1/blogs/creators/posts/{postId}/publish ───────────
        group.MapPost("/{postId:guid}/publish", async (
            Guid postId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new PublishCreatorPostCommand(postId), ct);
            return result.ToApiResult();
        })
        .WithName("PublishCreatorPost")
        .WithSummary("Directly publish a Draft post (Tier 1+ only)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Submit));

        // ── DELETE /api/v1/blogs/creators/posts/{postId} ─────────────────
        group.MapDelete("/{postId:guid}", async (
            Guid postId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCreatorPostCommand(postId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteCreatorPost")
        .WithSummary("Delete a Draft or Rejected post")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.CreatorPost, AppAction.Delete));
    }
}
