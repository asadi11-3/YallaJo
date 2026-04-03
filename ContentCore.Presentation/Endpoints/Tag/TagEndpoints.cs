using ContentCore.Application.Commands.Tag.CreateTag;
using ContentCore.Application.Commands.Tag.DeleteTag;
using ContentCore.Application.Commands.Tag.UpdateTag;
using ContentCore.Application.Queries.Tag.Common;
using ContentCore.Application.Queries.Tag.GetTagById;
using ContentCore.Application.Queries.Tag.ListTags;
using ContentCore.Presentation.Endpoints.Tag.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;

namespace ContentCore.Presentation.Endpoints.Tag;

internal static class TagEndpoints
{
    internal static void MapTagEndpoints(RouteGroupBuilder group)
    {
        var tags = group.MapGroup("/tags").WithTags("ContentCore | Tags");

        tags.MapGet("/", async (ISender sender, bool activeOnly = false) =>
        {
            var result = await sender.Send(new ListTagsQuery(activeOnly));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("ListTags")
        .Produces<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)
        .WithSummary("List tags")
        .AllowAnonymous();

        tags.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTagByIdQuery(id));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("GetTagById")
        .Produces<TagDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get tag by ID")
        .AllowAnonymous();

        tags.MapPost("/", async (CreateTagRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateTagCommand(request.Name, request.Slug));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("CreateTag")
        .Produces<CreateTagResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a tag")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Tag, AppAction.Create))
        .RequireAuthorization();

        tags.MapPut("/{id:guid}", async (Guid id, UpdateTagRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTagCommand(id, request.Name, request.Slug));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("UpdateTag")
        .Produces<UpdateTagResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a tag")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Tag, AppAction.Update))
        .RequireAuthorization();

        tags.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTagCommand(id));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("DeleteTag")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete a tag")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Tag, AppAction.Delete))
        .RequireAuthorization();
    }
}
