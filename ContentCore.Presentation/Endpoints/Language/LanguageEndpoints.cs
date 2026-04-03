using ContentCore.Application.Commands.Language.CreateLanguage;
using ContentCore.Application.Commands.Language.UpdateLanguage;
using ContentCore.Application.Queries.Language.ListLanguages;
using ContentCore.Presentation.Endpoints.Language.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;

namespace ContentCore.Presentation.Endpoints.Language;

internal static class LanguageEndpoints
{
    internal static void MapLanguageEndpoints(RouteGroupBuilder group)
    {
        var languages = group.MapGroup("/languages").WithTags("ContentCore | Languages");

        languages.MapGet("/", async (ISender sender, bool activeOnly = true) =>
        {
            var result = await sender.Send(new ListLanguagesQuery(activeOnly));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("ListLanguages")
        .Produces<IReadOnlyList<LanguageDto>>(StatusCodes.Status200OK)
        .WithSummary("List all active languages")
        .AllowAnonymous();

        languages.MapPost("/", async (CreateLanguageRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateLanguageCommand(
                request.Code, request.Name, request.NativeName, request.IsRtl));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("CreateLanguage")
        .Produces<CreateLanguageResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a new language")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Language, AppAction.Create))
        .RequireAuthorization();

        languages.MapPut("/{id:guid}", async (Guid id, UpdateLanguageRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateLanguageCommand(
                id, request.Name, request.NativeName, request.IsRtl, request.IsActive));
            return ContentCoreResultHelper.ToApiResult(result);
        })
        .WithName("UpdateLanguage")
        .Produces<UpdateLanguageResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update language settings")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Language, AppAction.Update))
        .RequireAuthorization();
    }
}
