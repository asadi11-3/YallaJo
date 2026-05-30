using ContentBlogs.Application.Commands.BlogComment.AddOrReplaceBlogCommentReaction;
using ContentBlogs.Application.Commands.BlogComment.CreateBlogComment;
using ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;
using ContentBlogs.Application.Commands.BlogComment.RemoveBlogCommentReaction;
using ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;
using ContentBlogs.Application.Queries.BlogComment.Dtos;
using ContentBlogs.Application.Queries.BlogComment.ListBlogComments;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Presentation.Endpoints.BlogComment.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentBlogs.Presentation.Endpoints.BlogComment;

internal static class BlogCommentEndpoints
{
    internal static void MapBlogCommentEndpoints(RouteGroupBuilder group)
    {
        // ── GET /api/v1/blogs/{id}/comments ───────────────────────────────────
        group.MapGet("/{id:guid}/comments", async (
            Guid id,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListBlogCommentsQuery(
                    BlogId: id,
                    Page: page ?? 1,
                    PageSize: pageSize ?? 20),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListBlogComments")
        .WithSummary("List comments for a Published blog (anonymous, paginated).")
        .Produces<PaginatedResult<BlogCommentDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── POST /api/v1/blogs/{id}/comments ──────────────────────────────────
        group.MapPost("/{id:guid}/comments", async (
            Guid id,
            CreateBlogCommentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateBlogCommentCommand(
                BlogId: id,
                Content: request.Content,
                ParentCommentId: request.ParentCommentId);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/blogs/comments/{r.CommentId}");
        })
        .WithName("CreateBlogComment")
        .WithSummary("Create a new comment (or reply) on a Published blog.")
        .Produces<CreateBlogCommentResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentBlogsFeatures.BlogComment, AppAction.Create));

        // ── PUT /api/v1/blogs/comments/{commentId} ────────────────────────────
        group.MapPut("/comments/{commentId:guid}", async (
            Guid commentId,
            UpdateBlogCommentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateBlogCommentCommand(
                CommentId: commentId,
                RowVersion: request.RowVersion,
                Content: request.Content);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateBlogComment")
        .WithSummary("Update a blog comment (owner ≤30 min, admin/superadmin anytime).")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentBlogsFeatures.BlogComment, AppAction.Update));

        // ── DELETE /api/v1/blogs/comments/{commentId} ─────────────────────────
        group.MapDelete("/comments/{commentId:guid}", async (
            Guid commentId,
            [FromBody] DeleteBlogCommentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new DeleteBlogCommentCommand(commentId, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteBlogComment")
        .WithSummary("Soft-delete a blog comment (content replaced with '[deleted]', replies preserved).")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentBlogsFeatures.BlogComment, AppAction.Delete));

        // ── POST /api/v1/blogs/comments/{commentId}/reactions ─────────────────
        group.MapPost("/comments/{commentId:guid}/reactions", async (
            Guid commentId,
            AddOrReplaceBlogCommentReactionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AddOrReplaceBlogCommentReactionCommand(
                CommentId: commentId,
                ReactionType: request.ReactionType);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AddOrReplaceBlogCommentReaction")
        .WithSummary("Add or replace the current user's reaction on a blog comment (idempotent).")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentBlogsFeatures.BlogReaction, AppAction.Create));

        // ── DELETE /api/v1/blogs/comments/{commentId}/reactions ───────────────
        group.MapDelete("/comments/{commentId:guid}/reactions", async (
            Guid commentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveBlogCommentReactionCommand(commentId), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveBlogCommentReaction")
        .WithSummary("Remove the current user's reaction on a blog comment (idempotent).")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(
            ContentBlogsFeatures.BlogReaction, AppAction.Delete));
    }
}
