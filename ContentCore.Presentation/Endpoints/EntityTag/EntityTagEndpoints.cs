using ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;
using ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;
using ContentCore.Application.Queries.EntityTag.GetEntityTags;
using ContentCore.Presentation.Endpoints.EntityTag.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.EntityTag;

internal static class EntityTagEndpoints
{
    internal static void MapEntityTagEndpoints(RouteGroupBuilder group)
    {
        var entityTags = group.MapGroup("/entity-tags").WithTags("ContentCore | Entity Tags");

        entityTags.MapGet("/", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityTagsQuery(entityType, entityId));
            return result.ToApiResult();
        })
        .WithName("GetEntityTags")
        .Produces<IReadOnlyList<EntityTagDto>>(StatusCodes.Status200OK)
        .WithSummary("Get tags assigned to an entity")
        .AllowAnonymous();

        entityTags.MapPost("/", async (AssignTagsToEntityRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AssignTagsToEntityCommand(
                request.EntityType,
                request.EntityId,
                request.TagIds));
            return result.ToApiResult();
        })
        .WithName("AssignTagsToEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Assign tags to an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.EntityTag, AppAction.Create))
        .RequireAuthorization();

        entityTags.MapDelete("/", async (string entityType, Guid entityId, Guid tagId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveTagFromEntityCommand(entityType, entityId, tagId));
            return result.ToApiResult();
        })
        .WithName("RemoveTagFromEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove tag assignment from an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.EntityTag, AppAction.Delete))
        .RequireAuthorization();
    }
}
