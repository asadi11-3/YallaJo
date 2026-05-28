using ContentCore.Application.Commands.Language.ActivateLanguage;
using ContentCore.Application.Commands.Language.CreateLanguage;
using ContentCore.Application.Commands.Language.DeactivateLanguage;
using ContentCore.Application.Commands.Language.DeleteLanguage;
using ContentCore.Application.Commands.Language.UpdateLanguage;
using ContentCore.Application.Queries.Language.GetLanguageById;
using ContentCore.Application.Queries.Language.ListLanguages;
using ContentCore.Presentation.Endpoints.Language.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Language;

internal static class LanguageEndpoints
{
    internal static void MapLanguageEndpoints(RouteGroupBuilder group)
    {
        var languages = group.MapGroup("/languages").WithTags("ContentCore | Languages");

        languages.MapGet("/", async (ISender sender, CancellationToken ct, bool activeOnly = true) =>
        {
            var result = await sender.Send(new ListLanguagesQuery(activeOnly), ct);
            return result.ToApiResult();
        })
        .WithName("ListLanguages")
        .Produces<IReadOnlyList<LanguageDto>>(StatusCodes.Status200OK)
        .WithSummary("List all active languages")
        .AllowAnonymous();

        languages.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLanguageByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetLanguageById")
        .Produces<LanguageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a language by ID")
        .AllowAnonymous();

        languages.MapPost("/", async (CreateLanguageRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new CreateLanguageCommand(
                request.Code, request.Name, request.NativeName, request.IsRtl), ct);
            return result.ToApiResult();
        })
        .WithName("CreateLanguage")
        .Produces<CreateLanguageResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a new language")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Language, AppAction.Create))
        .RequireAuthorization();

        languages.MapPut("/{id:guid}", async (Guid id, UpdateLanguageRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new UpdateLanguageCommand(
                id, request.Name, request.NativeName, request.IsRtl, request.IsActive), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateLanguage")
        .Produces<UpdateLanguageResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update language settings")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Language, AppAction.Update))
        .RequireAuthorization();

        languages.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeleteLanguageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteLanguage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a language")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Language, AppAction.Delete))
        .RequireAuthorization();

        languages.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new ActivateLanguageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ActivateLanguage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate a language — idempotent, no-op if already active")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Language, AppAction.Update))
        .RequireAuthorization();

        languages.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeactivateLanguageCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateLanguage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a language — idempotent, no-op if already inactive")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Language, AppAction.Update))
        .RequireAuthorization();
    }
}
