using ContentCore.Application.Commands.Category.CreateCategory;
using ContentCore.Application.Commands.Category.DeactivateCategory;
using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Application.Commands.Category.ReactivateCategory;
using ContentCore.Application.Commands.Category.ReorderCategories;
using ContentCore.Application.Commands.Category.UpdateCategory;
using ContentCore.Application.Queries.Category.Common;
using ContentCore.Application.Queries.Category.GetCategoryById;
using ContentCore.Application.Queries.Category.ListCategories;
using ContentCore.Presentation.Endpoints.Category.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Category;

internal static class CategoryEndpoints
{
    internal static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        var categories = group.MapGroup("/categories").WithTags("ContentCore | Categories");
        categories.MapGet("/", async (HttpContext http, ISender sender,
            Guid? parentCategoryId = null,
            CancellationToken ct = default) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(
                new ListCategoriesQuery(
                ActiveOnly: true,
                ParentCategoryId: parentCategoryId,
                WithTranslations: withTranslations), ct);
            return result.ToApiResult();
        })
        .WithName("ListCategories")
        .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
        .WithSummary("List active categories as a tree (public)")
        .AllowAnonymous();

        // GET /{id} — Single active category (public). Returns 404 for inactive categories.
        categories.MapGet("/{id:guid}", async (Guid id, HttpContext http, ISender sender,
            CancellationToken ct = default) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(
                new GetCategoryByIdQuery(id, withTranslations, IncludeInactive: false), ct);
            return result.ToApiResult();
        })
        .WithName("GetCategoryById")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get active category by ID with direct children (public)")
        .AllowAnonymous();

        // ── Admin endpoints — may access inactive categories ──────────────────────────

        // GET /admin — Admin list including inactive. Requires Permission.Category.Read.
        categories.MapGet("/admin", async (HttpContext http, ISender sender,
            Guid? parentCategoryId = null,
            bool activeOnly = false,
            CancellationToken ct = default) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(
                new ListCategoriesQuery(
                ActiveOnly: activeOnly,
                ParentCategoryId: parentCategoryId,
                WithTranslations: withTranslations), ct);
            return result.ToApiResult();
        })
        .WithName("ListCategoriesAdmin")
        .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
        .WithSummary("List categories (admin — may include inactive, requires read permission)")
        .RequireAuthorization("Permission.Category.Read");

        // GET /admin/{id} — Admin single category lookup including inactive.
        categories.MapGet("/admin/{id:guid}", async (Guid id, HttpContext http, ISender sender,
            bool includeInactive = true,
            CancellationToken ct = default) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new GetCategoryByIdQuery(id, withTranslations, includeInactive), ct);
            return result.ToApiResult();
        })
        .WithName("GetCategoryByIdAdmin")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get category by ID (admin — may include inactive)")
        .RequireAuthorization("Permission.Category.Read");

        // ── Write endpoints ────────────────────────────────────────────────────────────

        // POST / — Create category (admin only)
        categories.MapPost("/", async (CreateCategoryRequest request, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.ParentCategoryId,
                request.Icon,
                request.SortOrder,
                request.SourceLanguageCode ?? "en"), ct);
            return result.ToApiResult();
        })
        .WithName("CreateCategory")
        .Produces<CreateCategoryResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Create a category with auto-translation to all active languages")
        .RequireAuthorization("Permission.Category.Create");

        // PUT /{id} — Update category (admin only)
        categories.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new UpdateCategoryCommand(
                id,
                request.Name,
                request.Slug,
                request.ParentCategoryId,
                request.Icon,
                request.SortOrder,
                request.SourceLanguageCode ?? "en",
                request.Translations), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCategory")
        .Produces<UpdateCategoryResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a category with auto re-translation")
        .RequireAuthorization("Permission.Category.Update");

        // DELETE /{id} — Soft-delete category (admin only)
        categories.MapDelete("/{id:guid}", async (Guid id, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteCategory")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a category (sets IsDeleted = true)")
        .RequireAuthorization("Permission.Category.Delete");

        // PATCH /{id}/deactivate — Hide category from listings without deleting it
        categories.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeactivateCategoryCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateCategory")
        .Produces<DeactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate (hide) a category without deleting it")
        .RequireAuthorization("Permission.Category.Update");

        // PATCH /{id}/activate — Restore a deactivated category
        categories.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new ReactivateCategoryCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ActivateCategory")
        .Produces<ReactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate (restore) a deactivated category")
        .RequireAuthorization("Permission.Category.Update");

        // PUT /reorder — Batch sort order update (admin only)
        categories.MapPut("/reorder", async (ReorderCategoriesRequest request, ISender sender,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new ReorderCategoriesCommand(
                request.SortOrders
                    .Select(i => new CategorySortOrderUpdate(i.CategoryId, i.SortOrder))
                    .ToList()), ct);
            return result.ToApiResult();
        })
        .WithName("ReorderCategories")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder categories via batch sort order update")
        .RequireAuthorization("Permission.Category.Update");
    }
}
