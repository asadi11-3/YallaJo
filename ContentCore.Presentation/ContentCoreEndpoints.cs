using ContentCore.Application.Commands.Category.CreateCategory;
using ContentCore.Application.Commands.Category.DeactivateCategory;
using ContentCore.Application.Commands.Category.ReactivateCategory;
using ContentCore.Application.Commands.Category.ReorderCategories;
using ContentCore.Application.Commands.Category.UpdateCategory;
using ContentCore.Application.Commands.Language.CreateLanguage;
using ContentCore.Application.Commands.Language.UpdateLanguage;
using ContentCore.Application.Commands.Translation.ApproveTranslation;
using ContentCore.Application.Commands.Translation.BatchTranslate;
using ContentCore.Application.Commands.Translation.TranslateText;
using ContentCore.Application.Commands.Translation.UpdateTranslation;
using ContentCore.Application.Queries.Category.GetCategories;
using ContentCore.Application.Queries.Category.GetCategoryById;
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

        categories.MapGet("/", async (
            Guid? parentCategoryId,
            bool? isActive,
            ISender sender) =>
        {
            var result = await sender.Send(new GetCategoriesQuery(
                parentCategoryId,
                isActive));

            return ToApiResult(result);
        })
        .WithName("GetCategories")
        .Produces<GetCategoriesResponse>(StatusCodes.Status200OK)
        .WithSummary("List categories as a tree structure")
        .AllowAnonymous();

        categories.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCategoryByIdQuery(id));
            return ToApiResult(result);
        })
        .WithName("GetCategoryById")
        .Produces<GetCategoryByIdResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get single category with subcategories")
        .AllowAnonymous();

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

            return ToApiResult(result);
        })
        .WithName("UpdateCategory")
        .Produces<UpdateCategoryResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a category")
        .RequireAuthorization("Admin");

        categories.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateCategoryCommand(id));
            return ToApiResult(result);
        })
        .WithName("DeactivateCategory")
        .Produces<DeactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate category")
        .RequireAuthorization("Admin");

        categories.MapPatch("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ReactivateCategoryCommand(id));
            return ToApiResult(result);
        })
        .WithName("ReactivateCategory")
        .Produces<ReactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reactivate category")
        .RequireAuthorization("Admin");

        categories.MapPut("/reorder", async (ReorderCategoriesRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ReorderCategoriesCommand(
                request.Items
                    .Select(x => new ReorderCategoryItemDto(x.Id, x.SortOrder))
                    .ToList()));

            return ToApiResult(result);
        })
        .WithName("ReorderCategories")
        .Produces<ReorderCategoriesResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder categories")
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

public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    string? Icon = null,
    int? SortOrder = null,
    string? SourceLanguageCode = null,
    List<UpdateCategoryTranslationDto>? Translations = null);

public sealed record ReorderCategoryItemRequest(
    Guid Id,
    int SortOrder);

public sealed record ReorderCategoriesRequest(
    List<ReorderCategoryItemRequest> Items);