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
        var categories = group.MapGroup("/categories");

        // GET / — List categories as tree (public, includes translations when Accept-Language present)
        // ActiveOnly defaults to true so anonymous callers never see deactivated categories.
        // Authenticated admins may pass ?isActive=false to list all categories.
        categories.MapGet("/", async (HttpContext http, ISender sender,
            Guid? parentCategoryId = null,
            bool? isActive = null) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new ListCategoriesQuery(
                ActiveOnly: isActive ?? true,
                ParentCategoryId: parentCategoryId,
                WithTranslations: withTranslations));
            return result.ToApiResult();
        })
        .WithName("ListCategories")
        .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
        .WithSummary("List categories as a tree with optional translations")
        .AllowAnonymous();

        // GET /{id} — Get single category with direct children (public)
        categories.MapGet("/{id:guid}", async (Guid id, HttpContext http, ISender sender) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new GetCategoryByIdQuery(id, withTranslations));
            return result.ToApiResult();
        })
        .WithName("GetCategoryById")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get single category with direct children and translations")
        .AllowAnonymous();

        // POST / — Create category (admin only)
        categories.MapPost("/", async (CreateCategoryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.ParentCategoryId,
                request.Icon,
                request.SortOrder,
                request.SourceLanguageCode ?? "en"));
            return result.ToApiResult();
        })
        .WithName("CreateCategory")
        .Produces<CreateCategoryResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Create a category with auto-translation to all active languages")
        .RequireAuthorization("Permission.Category.Create");

        // PUT /{id} — Update category (admin only)
        categories.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateCategoryCommand(
                id,
                request.Name,
                request.Slug,
                request.ParentCategoryId,
                request.Icon,
                request.SortOrder,
                request.SourceLanguageCode ?? "en",
                request.Translations));
            return result.ToApiResult();
        })
        .WithName("UpdateCategory")
        .Produces<UpdateCategoryResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a category with auto re-translation")
        .RequireAuthorization("Permission.Category.Update");

        // DELETE /{id} — Soft-delete category (admin only)
        categories.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id));
            return result.ToApiResult();
        })
        .WithName("DeleteCategory")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a category (sets IsDeleted = true)")
        .RequireAuthorization("Permission.Category.Delete");

        // PATCH /{id}/deactivate — Hide category from listings without deleting it
        categories.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateCategoryCommand(id));
            return result.ToApiResult();
        })
        .WithName("DeactivateCategory")
        .Produces<DeactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate (hide) a category without deleting it")
        .RequireAuthorization("Permission.Category.Update");

        // PATCH /{id}/activate — Restore a deactivated category
        categories.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ReactivateCategoryCommand(id));
            return result.ToApiResult();
        })
        .WithName("ActivateCategory")
        .Produces<ReactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate (restore) a deactivated category")
        .RequireAuthorization("Permission.Category.Update");

        // PUT /reorder — Batch sort order update (admin only)
        categories.MapPut("/reorder", async (ReorderCategoriesRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ReorderCategoriesCommand(
                request.SortOrders
                    .Select(i => new CategorySortOrderUpdate(i.CategoryId, i.SortOrder))
                    .ToList()));
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
