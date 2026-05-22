using ContentBlogs.Application.Commands.Creator.Posts.ApprovePost;
using ContentBlogs.Application.Commands.Creator.Posts.DemoteTier;
using ContentBlogs.Application.Commands.Creator.Posts.FeaturePost;
using ContentBlogs.Application.Commands.Creator.Posts.PromoteTier;
using ContentBlogs.Application.Commands.Creator.Posts.RejectPost;
using ContentBlogs.Application.Commands.Creator.Posts.RemovePost;
using ContentBlogs.Application.Commands.Creator.Posts.UnfeaturePost;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Application.Queries.Creator.Posts.GetAdminPostQueue;
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

internal static class AdminPostEndpoints
{
    internal static void MapAdminPostEndpoints(RouteGroupBuilder blogGroup)
    {
        var group = blogGroup.MapGroup("/admin/creators/posts")
            .WithTags("Admin - Creator Posts");

        // ── GET /api/v1/blogs/admin/creators/posts ───────────────────────
        group.MapGet("/", async (
            CreatorPostStatus? status,
            CreatorPostType? type,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetAdminPostQueueQuery(status, type, page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("AdminGetPostQueue")
        .WithSummary("Admin — list creator posts for moderation (defaults to PendingReview)")
        .Produces<PaginatedResult<CreatorPostSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Read));

        // ── POST /api/v1/blogs/admin/creators/posts/{postId}/approve ─────
        group.MapPost("/{postId:guid}/approve", async (
            Guid postId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveCreatorPostCommand(postId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminApproveCreatorPost")
        .WithSummary("Admin — approve a pending-review post for publication")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Approve));

        // ── POST /api/v1/blogs/admin/creators/posts/{postId}/reject ──────
        group.MapPost("/{postId:guid}/reject", async (
            Guid postId,
            RejectCreatorPostRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RejectCreatorPostCommand(postId, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminRejectCreatorPost")
        .WithSummary("Admin — reject a pending-review post")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Reject));

        // ── POST /api/v1/blogs/admin/creators/posts/{postId}/feature ─────
        group.MapPost("/{postId:guid}/feature", async (
            Guid postId,
            FeatureCreatorPostRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new FeatureCreatorPostCommand(postId, request.FeaturedUntil);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminFeatureCreatorPost")
        .WithSummary("Admin — feature a published post from a Tier-2 creator")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Feature));

        // ── POST /api/v1/blogs/admin/creators/posts/{postId}/unfeature ───
        group.MapPost("/{postId:guid}/unfeature", async (
            Guid postId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnfeatureCreatorPostCommand(postId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminUnfeatureCreatorPost")
        .WithSummary("Admin — remove featuring from a post")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Feature));

        // ── POST /api/v1/blogs/admin/creators/posts/{postId}/remove ──────
        group.MapPost("/{postId:guid}/remove", async (
            Guid postId,
            RemoveCreatorPostRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RemoveCreatorPostCommand(postId, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminRemoveCreatorPost")
        .WithSummary("Admin — remove a published post")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminPostModeration, AppAction.Remove));
    }
}
