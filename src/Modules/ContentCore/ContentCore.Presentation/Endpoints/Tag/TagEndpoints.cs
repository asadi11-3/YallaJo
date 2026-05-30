using ContentCore.Application.Commands.Tag.ActivateTag;
using ContentCore.Application.Commands.Tag.CreateTag;
using ContentCore.Application.Commands.Tag.DeactivateTag;
using ContentCore.Application.Commands.Tag.DeleteTag;
using ContentCore.Application.Commands.Tag.UpdateTag;
using ContentCore.Application.Queries.Tag.Common;
using ContentCore.Application.Queries.Tag.GetTagById;
using ContentCore.Application.Queries.Tag.ListTags;
using ContentCore.Presentation.Endpoints.Tag.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Tag;

internal static class TagEndpoints
{
    internal static void MapTagEndpoints(RouteGroupBuilder group)
    {
        var tags = group.MapGroup("/tags").WithTags("ContentCore | Tags");

        tags.MapGet("/", async (HttpContext http, ISender sender, CancellationToken ct, bool activeOnly = false) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new ListTagsQuery(activeOnly, withTranslations), ct);
            return result.ToApiResult();
        })
        .WithName("ListTags")
        .Produces<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)
        .WithSummary("List tags (sends translations when Accept-Language header present)")
        .AllowAnonymous();

        tags.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetTagByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetTagById")
        .Produces<TagDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get tag by ID")
        .AllowAnonymous();

        tags.MapPost("/", async (CreateTagRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new CreateTagCommand(request.Name, request.Slug, request.SourceLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("CreateTag")
        .Produces<CreateTagResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a tag")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Create))
        .RequireAuthorization();

        tags.MapPut("/{id:guid}", async (Guid id, UpdateTagRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new UpdateTagCommand(id, request.Name, request.Slug, request.SourceLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTag")
        .Produces<UpdateTagResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a tag")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Update))
        .RequireAuthorization();

        tags.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeleteTagCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteTag")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete a tag")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Delete))
        .RequireAuthorization();

        tags.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new ActivateTagCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ActivateTag")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate a tag — idempotent, no-op if already active")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Update))
        .RequireAuthorization();

        tags.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeactivateTagCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateTag")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a tag — idempotent, no-op if already inactive")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Update))
        .RequireAuthorization();
    }
}
