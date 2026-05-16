using ContentBlogs.Application.Commands.Blog.ArchiveBlog;
using ContentBlogs.Application.Commands.Blog.CreateBlog;
using ContentBlogs.Application.Commands.Blog.DeleteBlog;
using ContentBlogs.Application.Commands.Blog.LinkBlogTours;
using ContentBlogs.Application.Commands.Blog.PublishBlog;
using ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;
using ContentBlogs.Application.Commands.Blog.UnpublishBlog;
using ContentBlogs.Application.Commands.Blog.UpdateBlog;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetAdminBlogById;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Application.Queries.Blog.GetBlogBySlug;
using ContentBlogs.Application.Queries.Blog.ListBlogs;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Presentation.Endpoints.Blog.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
                    AcceptLanguage: acceptLanguage),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListBlogs")
        .WithSummary("List public Published blogs with pagination and optional filters")
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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Read));

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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Create));

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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Update));
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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Delete));

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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));

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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));
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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));

        group.MapPost("/{id:guid}/views", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ContentBlogs.Application.Commands.Blog.IncrementBlogViewCount.IncrementBlogViewCountCommand(id),
                ct);
            return result.ToApiResult();
        })
        .WithName("IncrementBlogViewCount")
        .WithSummary("Atomically increment the view count for a Published blog")
        .Produces(StatusCodes.Status200OK)
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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.BlogTourLink, AppAction.Create));

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
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.BlogTourLink, AppAction.Delete));
    }
}
