using ContentBlogs.Application.Commands.Blog.ApproveBlog;
using ContentBlogs.Application.Commands.Blog.ArchiveBlog;
using ContentBlogs.Application.Commands.Blog.CreateBlog;
using ContentBlogs.Application.Commands.Blog.FeatureBlog;
using ContentBlogs.Application.Commands.Blog.HideBlog;
using ContentBlogs.Application.Commands.Blog.UnhideBlog;
using ContentBlogs.Application.Commands.Blog.DeleteBlog;
using ContentBlogs.Application.Commands.Blog.LinkBlogTours;
using ContentBlogs.Application.Commands.Blog.PublishBlog;
using ContentBlogs.Application.Commands.Blog.RejectBlog;
using ContentBlogs.Application.Commands.Blog.RemoveBlog;
using ContentBlogs.Application.Commands.Blog.RestoreBlog;
using ContentBlogs.Application.Commands.Blog.SubmitBlogForReview;
using ContentBlogs.Application.Commands.Blog.TrackBlogView;
using ContentBlogs.Application.Commands.Blog.UnfeatureBlog;
using ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;
using ContentBlogs.Application.Commands.Blog.UnpublishBlog;
using ContentBlogs.Application.Commands.Blog.UpdateBlog;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetAdminBlogById;
using ContentBlogs.Application.Queries.Blog.GetAdminBlogQueue;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Application.Queries.Blog.GetBlogBySlug;
using ContentBlogs.Application.Queries.Blog.GetDeletedBlogsAdmin;
using ContentBlogs.Application.Queries.Blog.GetMyBlogs;
using ContentBlogs.Application.Queries.Blog.ListBlogs;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Presentation.Endpoints.Blog.Models;
using ContentBlogs.Presentation.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentBlogs.Presentation.Endpoints.Blog;

internal static class BlogEndpoints
{
    internal static void MapBlogEndpoints(RouteGroupBuilder group)
    {
        // ── GET /api/v1/blogs ─────────────────────────────────────────────────
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            Guid? placeId,
            string? search,
            bool? isFeatured,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(
                new ListBlogsQuery(
                    Page:           page ?? 1,
                    PageSize:       pageSize ?? 20,
                    PlaceId:        placeId,
                    Search:         search,
                    IsFeatured:     isFeatured,
                    AcceptLanguage: acceptLanguage),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListBlogs")
        .WithSummary("List public Published blogs with pagination and optional filters (incl. isFeatured)")
        .Produces<PaginatedResult<BlogSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .AllowAnonymous();

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new GetBlogByIdQuery(id, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetBlogById")
        .WithSummary("Get a public Published blog by ID")
        .Produces<BlogDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapGet("/slug/{slug}", async (
            string slug,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new GetBlogBySlugQuery(slug, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetBlogBySlug")
        .WithSummary("Get a public Published blog by slug")
        .Produces<BlogDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapGet("/admin/{id:guid}", async (
            Guid id,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new GetAdminBlogByIdQuery(id, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetAdminBlogById")
        .WithSummary("Admin — get any non-deleted blog by ID with RowVersion (any status)")
        .Produces<AdminBlogDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Read));

        group.MapGet("/admin/deleted", async (
            int? pageNumber,
            int? pageSize,
            string? q,
            BlogStatus? status,
            Guid? placeId,
            string? sortBy,
            string? sortOrder,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetDeletedBlogsAdminQuery(
                    Page:      pageNumber ?? 1,
                    PageSize:  pageSize ?? 20,
                    Search:    q,
                    Status:    status,
                    PlaceId:   placeId,
                    SortBy:    sortBy,
                    SortOrder: sortOrder),
                ct);
            return result.ToApiResult();
        })
        .WithName("GetDeletedBlogsAdmin")
        .WithSummary("Admin — list soft-deleted blogs (with RowVersion for restore)")
        .Produces<PaginatedResult<AdminDeletedBlogListItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Delete));

        group.MapPost("/", async (
            CreateBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateBlogCommand(
                Title:              request.Title,
                Content:            request.Content,
                SourceLanguageCode: request.SourceLanguageCode,
                Slug:               request.Slug,
                Summary:            request.Summary,
                MetaTitle:          request.MetaTitle,
                MetaDescription:    request.MetaDescription,
                PlaceId:            request.PlaceId);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/blogs/{r.BlogId}");
        })
        .WithName("CreateBlog")
        .WithSummary("Create a new Blog in Draft status")
        .Produces<CreateBlogResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Create));

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateBlogCommand(
                BlogId:          id,
                RowVersion:      request.RowVersion,
                Title:           request.Title,
                Slug:            request.Slug,
                Content:         request.Content,
                Summary:         request.Summary,
                MetaTitle:       request.MetaTitle,
                MetaDescription: request.MetaDescription,
                PlaceId:         request.PlaceId,
                ReadTimeMinutes: request.ReadTimeMinutes);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateBlog")
        .WithSummary("Update a Draft or Published blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Update));
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromBody] BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteBlog")
        .WithSummary("Soft-delete a blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Delete));

        group.MapPost("/{id:guid}/restore", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RestoreBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("RestoreBlog")
        .WithSummary("Restore a soft-deleted blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Delete));

        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new PublishBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("PublishBlog")
        .WithSummary("Publish a Draft blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));

        group.MapPost("/{id:guid}/unpublish", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnpublishBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("UnpublishBlog")
        .WithSummary("Unpublish a Published blog (back to Draft)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchiveBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("ArchiveBlog")
        .WithSummary("Archive a Published blog (read-only state)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));

        // ── POST /{id}/hide ───────────────────────────────────────────────
        group.MapPost("/{id:guid}/hide", async (
            Guid id,
            HideBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new HideBlogCommand(id, request.RowVersion, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("HideBlog")
        .WithSummary("Hide a Published blog (admin moderation)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));

        // ── POST /{id}/unhide ─────────────────────────────────────────────
        group.MapPost("/{id:guid}/unhide", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UnhideBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("UnhideBlog")
        .WithSummary("Unhide a Hidden blog (restore to Published)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));

        group.MapPost("/{id:guid}/views", async (
            Guid id,
            ICurrentUser currentUser,
            AnonymousViewerProvider anonymousViewer,
            ISender sender,
            CancellationToken ct) =>
        {
            var (kind, viewerId) = currentUser.IsAuthenticated && currentUser.UserId.HasValue
                ? (BlogViewerKind.Authenticated, currentUser.UserId.Value.ToString())
                : (BlogViewerKind.Anonymous,    anonymousViewer.GetOrCreate());

            var result = await sender.Send(
                new TrackBlogViewCommand(id, kind, viewerId),
                ct);
            return result.ToApiResult();
        })
        .WithName("TrackBlogView")
        .WithSummary("Track a unique blog view (lifetime-debounced per viewer)")
        .Produces<BlogViewCountResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapPost("/{id:guid}/tours", async (
            Guid id,
            BlogLinkToursRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new LinkBlogToursCommand(
                BlogId:     id,
                RowVersion: request.RowVersion,
                Tours:      request.Tours
                    .Select(item => new LinkBlogTourItem(item.TourId, item.SortOrder))
                    .ToList());

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("LinkBlogTours")
        .WithSummary("Link one or more Tours to a Blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.BlogTourLink, AppAction.Create));

        group.MapDelete("/{id:guid}/tours/{tourId:guid}", async (
            Guid id,
            Guid tourId,
            [FromBody] BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UnlinkBlogFromTourCommand(
                BlogId:     id,
                TourId:     tourId,
                RowVersion: request.RowVersion);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UnlinkBlogFromTour")
        .WithSummary("Remove a Blog ↔ Tour link")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.BlogTourLink, AppAction.Delete));

        // ── Creator: POST /{id}/submit-for-review ────────────────────────
        group.MapPost("/{id:guid}/submit-for-review", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitBlogForReviewCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitBlogForReview")
        .WithSummary("Creator — submit a Draft blog for admin review")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Submit));

        // ── Creator: GET /my-blogs ────────────────────────────────────────
        group.MapGet("/my-blogs", async (
            int? page,
            int? pageSize,
            string? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetMyBlogsQuery(Page: page ?? 1, PageSize: pageSize ?? 20, StatusFilter: status),
                ct);
            return result.ToApiResult();
        })
        .WithName("GetMyBlogs")
        .WithSummary("Creator — list my own blogs with optional status filter")
        .Produces<PaginatedResult<BlogSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.ReadOwn));

        // ── Admin: GET /admin/queue ───────────────────────────────────────
        group.MapGet("/admin/queue", async (
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetAdminBlogQueueQuery(Page: page ?? 1, PageSize: pageSize ?? 20),
                ct);
            return result.ToApiResult();
        })
        .WithName("GetAdminBlogQueue")
        .WithSummary("Admin — view PendingReview blog moderation queue")
        .Produces<PaginatedResult<BlogSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminBlogQueue, AppAction.Read));

        // ── Admin: POST /admin/{id}/approve ──────────────────────────────
        group.MapPost("/admin/{id:guid}/approve", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveBlog")
        .WithSummary("Admin — approve a PendingReview blog (publishes it)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Approve));

        // ── Admin: POST /admin/{id}/reject ───────────────────────────────
        group.MapPost("/admin/{id:guid}/reject", async (
            Guid id,
            RejectBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectBlogCommand(id, request.RowVersion, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("RejectBlog")
        .WithSummary("Admin — reject a PendingReview blog with a reason")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Reject));

        // ── Admin: POST /admin/{id}/remove ───────────────────────────────
        group.MapPost("/admin/{id:guid}/remove", async (
            Guid id,
            RemoveBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveBlogCommand(id, request.RowVersion, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveBlog")
        .WithSummary("Admin — permanently remove a Published blog for policy violations")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Remove));

        // ── Admin: POST /{id}/feature ─────────────────────────────────────
        group.MapPost("/{id:guid}/feature", async (
            Guid id,
            FeatureBlogRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new FeatureBlogCommand(id, request.RowVersion, request.FeaturedUntil), ct);
            return result.ToApiResult();
        })
        .WithName("FeatureBlog")
        .WithSummary("Admin — feature a Published blog with optional expiry")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Feature));

        // ── Admin: POST /{id}/unfeature ───────────────────────────────────
        group.MapPost("/{id:guid}/unfeature", async (
            Guid id,
            BlogRowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnfeatureBlogCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("UnfeatureBlog")
        .WithSummary("Admin — remove feature status from a blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Blog, AppAction.Unfeature));
    }
}
