using ContentCore.Application.Commands.Category.CreateCategory;
using ContentCore.Application.Commands.Language.CreateLanguage;
using ContentCore.Application.Commands.Language.UpdateLanguage;
using ContentCore.Application.Commands.Translation.ApproveTranslation;
using ContentCore.Application.Commands.Translation.BatchTranslate;
using ContentCore.Application.Commands.Translation.TranslateText;
using ContentCore.Application.Commands.Translation.UpdateTranslation;
using ContentCore.Application.Queries.Language.ListLanguages;
using ContentCore.Application.Queries.Translation.GetEntityTranslations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Presentation;

public static class ContentCoreEndpoints
{
    public static IEndpointRouteBuilder MapContentCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/content-core")
            .WithTags("ContentCore");

        MapLanguageEndpoints(group);
        MapTranslationEndpoints(group);
        MapCategoryEndpoints(group);

        return endpoints;
    }

    // ── Category Endpoints ──────────────────────────────────────────────────

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        var categories = group.MapGroup("/categories");

        categories.MapPost("/", async (CreateCategoryRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.ParentCategoryId,
                request.Icon,
                request.SortOrder,
                request.SourceLanguageCode ?? "en"));
            return ToApiResult(result);
        })
        .WithName("CreateCategory")
        .Produces<CreateCategoryResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Create a category with auto-translation to all active languages")
        .RequireAuthorization("Admin");
    }

    // ── Language Endpoints ──────────────────────────────────────────────────

    private static void MapLanguageEndpoints(RouteGroupBuilder group)
    {
        var languages = group.MapGroup("/languages");

        languages.MapGet("/", async (ISender sender, bool activeOnly = true) =>
        {
            var result = await sender.Send(new ListLanguagesQuery(activeOnly));
            return ToApiResult(result);
        })
        .WithName("ListLanguages")
        .Produces<IReadOnlyList<LanguageDto>>(StatusCodes.Status200OK)
        .WithSummary("List all active languages")
        .AllowAnonymous();

        languages.MapPost("/", async (CreateLanguageRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateLanguageCommand(
                request.Code, request.Name, request.NativeName, request.IsRtl));
            return ToApiResult(result);
        })
        .WithName("CreateLanguage")
        .Produces<CreateLanguageResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a new language")
        .RequireAuthorization("Admin");

        languages.MapPut("/{id:guid}", async (Guid id, UpdateLanguageRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateLanguageCommand(
                id, request.Name, request.NativeName, request.IsRtl, request.IsActive));
            return ToApiResult(result);
        })
        .WithName("UpdateLanguage")
        .Produces<UpdateLanguageResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update language settings")
        .RequireAuthorization("Admin");
    }

    // ── Translation Endpoints ───────────────────────────────────────────────

    private static void MapTranslationEndpoints(RouteGroupBuilder group)
    {
        var translations = group.MapGroup("/translations");

        translations.MapPost("/translate", async (TranslateRequest request, ISender sender) =>
        {
            var result = await sender.Send(new TranslateTextCommand(
                request.Text, request.FromLanguageCode, request.ToLanguageCode));
            return ToApiResult(result);
        })
        .WithName("TranslateText")
        .Produces<TranslateTextResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("On-demand translation (admin tool)")
        .RequireAuthorization("Admin");

        translations.MapPost("/batch", async (BatchTranslateRequest request, ISender sender) =>
        {
            var result = await sender.Send(new BatchTranslateCommand(
                request.Texts, request.FromLanguageCode, request.ToLanguageCode));
            return ToApiResult(result);
        })
        .WithName("BatchTranslate")
        .Produces<BatchTranslateResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Batch translate multiple texts")
        .RequireAuthorization("Admin");

        translations.MapGet("/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityTranslationsQuery(entityType, entityId));
            return ToApiResult(result);
        })
        .WithName("GetEntityTranslations")
        .Produces<IReadOnlyList<EntityTranslationDto>>(StatusCodes.Status200OK)
        .WithSummary("Get all translations for an entity")
        .AllowAnonymous();

        translations.MapPut("/{id:guid}", async (Guid id, UpdateTranslationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTranslationCommand(id, request.TranslatedText));
            return ToApiResult(result);
        })
        .WithName("UpdateTranslation")
        .Produces<UpdateTranslationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update/override a translation")
        .RequireAuthorization("Admin");

        translations.MapPost("/{id:guid}/approve", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ApproveTranslationCommand(id));
            return ToApiResult(result);
        })
        .WithName("ApproveTranslation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark translation as human-reviewed")
        .RequireAuthorization("Admin");
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToApiResult(Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToProblem(Outcome outcome, IReadOnlyList<Error> errors)
    {
        var first = errors.Count > 0 ? errors[0] : null;
        return Results.Problem(
            statusCode: (int)outcome,
            title: first?.Code,
            detail: first?.Message);
    }
}

// ── Request DTOs ────────────────────────────────────────────────────────

public sealed record CreateLanguageRequest(string Code, string Name, string NativeName, bool IsRtl);
public sealed record UpdateLanguageRequest(string Name, string NativeName, bool IsRtl, bool IsActive);
public sealed record TranslateRequest(string Text, string FromLanguageCode, string ToLanguageCode);
public sealed record BatchTranslateRequest(IReadOnlyList<string> Texts, string FromLanguageCode, string ToLanguageCode);
public sealed record UpdateTranslationRequest(string TranslatedText);
public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int SortOrder = 0,
    string? SourceLanguageCode = null);
