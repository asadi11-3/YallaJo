using ContentCore.Application.Commands.Translation.ApproveTranslation;
using ContentCore.Application.Commands.Translation.BatchApproveTranslations;
using ContentCore.Application.Commands.Translation.BatchTranslate;
using ContentCore.Application.Commands.Translation.TranslateText;
using ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;
using ContentCore.Application.Commands.Translation.UpdateTranslation;
using ContentCore.Application.Queries.Translation.GetEntityTranslations;
using ContentCore.Domain.Enums;
using ContentCore.Presentation.Endpoints.Translation.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Translation;

internal static class TranslationEndpoints
{
    internal static void MapTranslationEndpoints(RouteGroupBuilder group)
    {
        var translations = group.MapGroup("/translations").WithTags("ContentCore | Translations");

        translations.MapPost("/translate", async (TranslateRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new TranslateTextCommand(
                request.Text, request.FromLanguageCode, request.ToLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("TranslateText")
        .Produces<TranslateTextResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("On-demand translation (admin tool)")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Create))
        .RequireAuthorization();

        translations.MapPost("/batch", async (BatchTranslateRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new BatchTranslateCommand(
                request.Texts, request.FromLanguageCode, request.ToLanguageCode), ct);
            return result.ToApiResult();
        })
        .WithName("BatchTranslate")
        .Produces<BatchTranslateResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Batch translate multiple texts")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Create))
        .RequireAuthorization();

        translations.MapGet("/{entityType}/{entityId:guid}", async (
            string entityType,
            Guid entityId,
            ISender sender,
            string? languageCode = null,
            TranslationStatus? status = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetEntityTranslationsQuery(entityType, entityId, languageCode, status), ct);
            return result.ToApiResult();
        })
        .WithName("GetEntityTranslations")
        .Produces<IReadOnlyList<EntityTranslationDto>>(StatusCodes.Status200OK)
        .WithSummary("Get translations for an entity (optional: ?languageCode=ar&status=AutoTranslated). Public.")
        .AllowAnonymous();

        translations.MapPut("/{id:guid}", async (Guid id, UpdateTranslationRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new UpdateTranslationCommand(id, request.TranslatedText), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTranslation")
        .Produces<UpdateTranslationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update/override a translation")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Update))
        .RequireAuthorization();

        translations.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new ApproveTranslationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveTranslation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark translation as human-reviewed")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Update))
        .RequireAuthorization();

        translations.MapPost("/backfill/{entityKind}", async (string entityKind, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new TriggerTranslationBackfillCommand(entityKind), ct);
            return result.ToApiResult();
        })
        .WithName("TriggerTranslationBackfill")
        .Produces<TriggerTranslationBackfillResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Backfill missing translations for all existing Tag or Specialization rows (entityKind: tag | specialization)")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Create))
        .RequireAuthorization();

        translations.MapPost("/approve-batch", async (BatchApproveTranslationsRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new BatchApproveTranslationsCommand(
                request.EntityType,
                request.EntityId,
                request.LanguageCode,
                request.FieldNames), ct);
            return result.ToApiResult();
        })
        .WithName("BatchApproveTranslations")
        .Produces<BatchApproveTranslationsResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Mark all auto-translated fields as human-reviewed for an entity + language in one shot")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.TranslationCache, AppAction.Update))
        .RequireAuthorization();
    }
}
