using ContentCore.Application.Commands.Translation.ApproveTranslation;
using ContentCore.Application.Commands.Translation.BatchTranslate;
using ContentCore.Application.Commands.Translation.TranslateText;
using ContentCore.Application.Commands.Translation.UpdateTranslation;
using ContentCore.Application.Queries.Translation.GetEntityTranslations;
using ContentCore.Presentation.Endpoints.Translation.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Translation;

internal static class TranslationEndpoints
{
    internal static void MapTranslationEndpoints(RouteGroupBuilder group)
    {
        var translations = group.MapGroup("/translations").WithTags("ContentCore | Translations");

        translations.MapPost("/translate", async (TranslateRequest request, ISender sender) =>
        {
            var result = await sender.Send(new TranslateTextCommand(
                request.Text, request.FromLanguageCode, request.ToLanguageCode));
            return result.ToApiResult();
        })
        .WithName("TranslateText")
        .Produces<TranslateTextResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("On-demand translation (admin tool)")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.TranslationCache, AppAction.Create))
        .RequireAuthorization();

        translations.MapPost("/batch", async (BatchTranslateRequest request, ISender sender) =>
        {
            var result = await sender.Send(new BatchTranslateCommand(
                request.Texts, request.FromLanguageCode, request.ToLanguageCode));
            return result.ToApiResult();
        })
        .WithName("BatchTranslate")
        .Produces<BatchTranslateResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Batch translate multiple texts")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.TranslationCache, AppAction.Create))
        .RequireAuthorization();

        translations.MapGet("/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityTranslationsQuery(entityType, entityId));
            return result.ToApiResult();
        })
        .WithName("GetEntityTranslations")
        .Produces<IReadOnlyList<EntityTranslationDto>>(StatusCodes.Status200OK)
        .WithSummary("Get all translations for an entity")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.TranslationCache, AppAction.Read))
        .RequireAuthorization();

        translations.MapPut("/{id:guid}", async (Guid id, UpdateTranslationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTranslationCommand(id, request.TranslatedText));
            return result.ToApiResult();
        })
        .WithName("UpdateTranslation")
        .Produces<UpdateTranslationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update/override a translation")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.TranslationCache, AppAction.Update))
        .RequireAuthorization();

        translations.MapPost("/{id:guid}/approve", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ApproveTranslationCommand(id));
            return result.ToApiResult();
        })
        .WithName("ApproveTranslation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark translation as human-reviewed")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.TranslationCache, AppAction.Update))
        .RequireAuthorization();
    }
}
