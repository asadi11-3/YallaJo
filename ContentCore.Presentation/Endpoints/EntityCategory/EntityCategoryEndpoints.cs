using ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;
using ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;
using ContentCore.Application.Queries.EntityCategory.GetEntityCategories;
using ContentCore.Presentation.Endpoints.EntityCategory.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.EntityCategory;

internal static class EntityCategoryEndpoints
{
    internal static void MapEntityCategoryEndpoints(RouteGroupBuilder group)
    {
        var entityCategories = group.MapGroup("/entity-categories").WithTags("ContentCore | Entity Categories");

        entityCategories.MapGet("/", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityCategoriesQuery(entityType, entityId));
            return result.ToApiResult();
        })
        .WithName("GetEntityCategories")
        .Produces<IReadOnlyList<EntityCategoryDto>>(StatusCodes.Status200OK)
        .WithSummary("Get categories assigned to an entity")
        .AllowAnonymous();

        entityCategories.MapPost("/", async (AssignCategoriesToEntityRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AssignCategoriesToEntityCommand(
                request.EntityType,
                request.EntityId,
                request.CategoryIds));
            return result.ToApiResult();
        })
        .WithName("AssignCategoriesToEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Assign categories to an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.EntityCategory, AppAction.Create))
        .RequireAuthorization();

        entityCategories.MapDelete("/", async (string entityType, Guid entityId, Guid categoryId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveCategoryFromEntityCommand(entityType, entityId, categoryId));
            return result.ToApiResult();
        })
        .WithName("RemoveCategoryFromEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove category assignment from an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.EntityCategory, AppAction.Delete))
        .RequireAuthorization();
    }
}
