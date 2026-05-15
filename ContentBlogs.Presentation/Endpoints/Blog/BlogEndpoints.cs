using ContentBlogs.Application.Commands.Blog.ArchiveBlog;
using ContentBlogs.Application.Commands.Blog.CreateBlog;
using ContentBlogs.Application.Commands.Blog.DeleteBlog;
using ContentBlogs.Application.Commands.Blog.PublishBlog;
using ContentBlogs.Application.Commands.Blog.UnpublishBlog;
using ContentBlogs.Application.Commands.Blog.UpdateBlog;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Application.Queries.Blog.GetBlogBySlug;
using ContentBlogs.Application.Queries.Blog.ListBlogs;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Presentation.Endpoints.Blog.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteBlogCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteBlog")
        .WithSummary("Soft-delete a blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Delete));

        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new PublishBlogCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("PublishBlog")
        .WithSummary("Publish a Draft blog")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));

        group.MapPost("/{id:guid}/unpublish", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnpublishBlogCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("UnpublishBlog")
        .WithSummary("Unpublish a Published blog (back to Draft)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));

        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchiveBlogCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ArchiveBlog")
        .WithSummary("Archive a Published blog (read-only state)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogFeatures.Blog, AppAction.Approve));
    }
}
