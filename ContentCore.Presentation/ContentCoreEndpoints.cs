using ContentCore.Application.Commands.Attachment.DeleteAttachment;
using ContentCore.Application.Commands.Attachment.ReorderAttachments;
using ContentCore.Application.Commands.Attachment.SetPrimaryImage;
using ContentCore.Application.Commands.Attachment.UploadAttachment;
using ContentCore.Application.Commands.Category.CreateCategory;
using ContentCore.Application.Commands.Category.DeactivateCategory;
using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Application.Commands.Category.ReactivateCategory;
using ContentCore.Application.Commands.Category.ReorderCategories;
using ContentCore.Application.Commands.Category.UpdateCategory;
using ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;
using ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;
using ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;
using ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;
using ContentCore.Application.Commands.Language.CreateLanguage;
using ContentCore.Application.Commands.Language.UpdateLanguage;
using ContentCore.Application.Commands.Specialization.CreateSpecialization;
using ContentCore.Application.Commands.Specialization.UpdateSpecialization;
using ContentCore.Application.Commands.Tag.CreateTag;
using ContentCore.Application.Commands.Tag.DeleteTag;
using ContentCore.Application.Commands.Tag.UpdateTag;
using ContentCore.Application.Commands.Translation.ApproveTranslation;
using ContentCore.Application.Commands.Translation.BatchTranslate;
using ContentCore.Application.Commands.Translation.TranslateText;
using ContentCore.Application.Commands.Translation.UpdateTranslation;
using ContentCore.Application.Queries.Attachment.GetAttachmentById;
using ContentCore.Application.Queries.Attachment.GetEntityAttachments;
using ContentCore.Application.Queries.Category.GetCategoryById;
using ContentCore.Application.Queries.Category.ListCategories;
using ContentCore.Application.Queries.EntityCategory.GetEntityCategories;
using ContentCore.Application.Queries.EntityTag.GetEntityTags;
using ContentCore.Application.Queries.Language.ListLanguages;
using ContentCore.Application.Queries.Specialization.ListSpecializations;
using ContentCore.Application.Queries.Tag.GetTagById;
using ContentCore.Application.Queries.Tag.ListTags;
using ContentCore.Application.Queries.Translation.GetEntityTranslations;
using ContentCore.Domain.Enums;
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
        var group = endpoints.MapGroup("/api/v1/content-core")
            .WithTags("ContentCore");

        MapLanguageEndpoints(group);
        MapTranslationEndpoints(group);
        MapCategoryEndpoints(group);
        MapAttachmentEndpoints(group);
        MapTagEndpoints(group);
        MapEntityCategoryEndpoints(group);
        MapEntityTagEndpoints(group);
        MapSpecializationEndpoints(group);

        return endpoints;
    }

    // ── Category Endpoints ──────────────────────────────────────────────────

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        var categories = group.MapGroup("/categories");

        // GET / — List categories as tree (public, includes translations when Accept-Language present)
        categories.MapGet("/", async (HttpContext http, ISender sender,
            Guid? parentCategoryId = null,
            bool? isActive = null) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new ListCategoriesQuery(
                ActiveOnly: isActive ?? false,
                ParentCategoryId: parentCategoryId,
                WithTranslations: withTranslations));
            return ToApiResult(result);
        })
        .WithName("ListCategories")
        .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
        .WithSummary("List categories as a tree with optional translations")
        .AllowAnonymous();

        // GET /{id} — Get single category with direct children (public)
        categories.MapGet("/{id:guid}", async (Guid id, HttpContext http, ISender sender) =>
        {
            var withTranslations = http.Request.Headers.AcceptLanguage.Count > 0;
            var result = await sender.Send(new GetCategoryByIdQuery(id, withTranslations));
            return ToApiResult(result);
        })
        .WithName("GetCategoryById")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get single category with direct children and translations")
        .AllowAnonymous();

        // POST / — Create category (admin only)
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
        .RequireAuthorization("Permission.Category.Create");

        // PUT /{id} — Update category (admin only)
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
        .WithSummary("Update a category with auto re-translation")
        .RequireAuthorization("Admin");

        // DELETE /{id} — Soft-delete category (admin only)
        categories.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id));
            return ToApiResult(result);
        })
        .WithName("DeleteCategory")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a category (sets IsDeleted = true)")
        .RequireAuthorization("Admin");

        // PATCH /{id}/deactivate — Hide category from listings without deleting it
        categories.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateCategoryCommand(id));
            return ToApiResult(result);
        })
        .WithName("DeactivateCategory")
        .Produces<DeactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate (hide) a category without deleting it")
        .RequireAuthorization("Admin");

        // PATCH /{id}/activate — Restore a deactivated category
        categories.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ReactivateCategoryCommand(id));
            return ToApiResult(result);
        })
        .WithName("ActivateCategory")
        .Produces<ReactivateCategoryResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Activate (restore) a deactivated category")
        .RequireAuthorization("Admin");

        // PUT /reorder — Batch sort order update (admin only)
        categories.MapPut("/reorder", async (ReorderCategoriesRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ReorderCategoriesCommand(
                request.Items.Select(i => new CategoryOrderItem(i.CategoryId, i.SortOrder)).ToList()));
            return ToApiResult(result);
        })
        .WithName("ReorderCategories")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder categories via batch sort order update")
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

    // ── Attachment Endpoints ────────────────────────────────────────────────────

    private static void MapAttachmentEndpoints(RouteGroupBuilder group)
    {
        var attachments = group.MapGroup("/attachments");

        // Upload attachment (multipart/form-data)
        attachments.MapPost("/", async (IFormFile file, [AsParameters] UploadAttachmentRequest request, ISender sender, HttpContext http) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            if (!Enum.TryParse<Domain.Enums.AttachmentType>(request.AttachmentType, true, out var attachmentType))
                return Results.BadRequest("Invalid AttachmentType.");

            var userId = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var uid) ? uid : Guid.Empty;

            await using var stream = file.OpenReadStream();
            var result = await sender.Send(new UploadAttachmentCommand(
                stream,
                file.FileName,
                file.ContentType,
                entityType,
                request.EntityId,
                attachmentType,
                userId,
                request.Width,
                request.Height,
                request.DurationSeconds,
                request.SortOrder));
            return ToApiResult(result);
        })
        .WithName("UploadAttachment")
        .Produces<UploadAttachmentResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Upload a file attachment for an entity")
        .RequireAuthorization()
        .DisableAntiforgery();

        // Get attachments by entity
        attachments.MapGet("/", async (EntityType entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityAttachmentsQuery(entityType, entityId));
            return ToApiResult(result);
        })
        .WithName("GetEntityAttachments")
        .Produces<IReadOnlyList<AttachmentDto>>(StatusCodes.Status200OK)
        .WithSummary("List attachments for an entity")
        .AllowAnonymous();

        // Get attachment by ID
        attachments.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetAttachmentByIdQuery(id));
            return ToApiResult(result);
        })
        .WithName("GetAttachmentById")
        .Produces<AttachmentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a single attachment by ID")
        .AllowAnonymous();

        // Delete attachment
        attachments.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteAttachmentCommand(id));
            return ToApiResult(result);
        })
        .WithName("DeleteAttachment")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete an attachment and its file")
        .RequireAuthorization();

        // Reorder attachments
        attachments.MapPut("/reorder", async (ReorderAttachmentsRequest request, ISender sender) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(new ReorderAttachmentsCommand(
                entityType, request.EntityId, request.OrderedAttachmentIds));
            return ToApiResult(result);
        })
        .WithName("ReorderAttachments")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder attachments for an entity")
        .RequireAuthorization();

        // Set primary image
        attachments.MapPut("/primary", async (SetPrimaryImageRequest request, ISender sender) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(new SetPrimaryImageCommand(
                entityType, request.EntityId, request.AttachmentId));
            return ToApiResult(result);
        })
        .WithName("SetPrimaryImage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Set an attachment as the primary image for an entity")
        .RequireAuthorization();
    }

    // ── Tag Endpoints ─────────────────────────────────────────────────────

    private static void MapTagEndpoints(RouteGroupBuilder group)
    {
        var tags = group.MapGroup("/tags");

        tags.MapGet("/", async (ISender sender, bool activeOnly = false) =>
        {
            var result = await sender.Send(new ListTagsQuery(activeOnly));
            return ToApiResult(result);
        })
        .WithName("ListTags")
        .Produces<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)
        .WithSummary("List tags")
        .AllowAnonymous();

        tags.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTagByIdQuery(id));
            return ToApiResult(result);
        })
        .WithName("GetTagById")
        .Produces<TagDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get tag by ID")
        .AllowAnonymous();

        tags.MapPost("/", async (CreateTagRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateTagCommand(request.Name, request.Slug));
            return ToApiResult(result);
        })
        .WithName("CreateTag")
        .Produces<CreateTagResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a tag")
        .RequireAuthorization("Admin");

        tags.MapPut("/{id:guid}", async (Guid id, UpdateTagRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateTagCommand(id, request.Name, request.Slug));
            return ToApiResult(result);
        })
        .WithName("UpdateTag")
        .Produces<UpdateTagResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a tag")
        .RequireAuthorization("Admin");

        tags.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTagCommand(id));
            return ToApiResult(result);
        })
        .WithName("DeleteTag")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete a tag")
        .RequireAuthorization("Admin");
    }

    // ── EntityCategory Endpoints ──────────────────────────────────────────

    private static void MapEntityCategoryEndpoints(RouteGroupBuilder group)
    {
        var entityCategories = group.MapGroup("/entity-categories");

        entityCategories.MapGet("/", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityCategoriesQuery(entityType, entityId));
            return ToApiResult(result);
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
            return ToApiResult(result);
        })
        .WithName("AssignCategoriesToEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Assign categories to an entity")
        .RequireAuthorization("Admin");

        entityCategories.MapDelete("/", async (string entityType, Guid entityId, Guid categoryId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveCategoryFromEntityCommand(entityType, entityId, categoryId));
            return ToApiResult(result);
        })
        .WithName("RemoveCategoryFromEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove category assignment from an entity")
        .RequireAuthorization("Admin");
    }

    // ── EntityTag Endpoints ───────────────────────────────────────────────

    private static void MapEntityTagEndpoints(RouteGroupBuilder group)
    {
        var entityTags = group.MapGroup("/entity-tags");

        entityTags.MapGet("/", async (string entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityTagsQuery(entityType, entityId));
            return ToApiResult(result);
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
            return ToApiResult(result);
        })
        .WithName("AssignTagsToEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Assign tags to an entity")
        .RequireAuthorization("Admin");

        entityTags.MapDelete("/", async (string entityType, Guid entityId, Guid tagId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveTagFromEntityCommand(entityType, entityId, tagId));
            return ToApiResult(result);
        })
        .WithName("RemoveTagFromEntity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove tag assignment from an entity")
        .RequireAuthorization("Admin");
    }

    // ── Specialization Endpoints ─────────────────────────────────────────

    private static void MapSpecializationEndpoints(RouteGroupBuilder group)
    {
        var specializations = group.MapGroup("/specializations");

        specializations.MapGet("/", async (ISender sender, bool activeOnly = false) =>
        {
            var result = await sender.Send(new ListSpecializationsQuery(activeOnly));
            return ToApiResult(result);
        })
        .WithName("ListSpecializations")
        .Produces<IReadOnlyList<SpecializationDto>>(StatusCodes.Status200OK)
        .WithSummary("List all specializations")
        .AllowAnonymous();

        specializations.MapPost("/", async (CreateSpecializationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateSpecializationCommand(
                request.Name,
                request.Description,
                request.Icon));
            return ToApiResult(result);
        })
        .WithName("CreateSpecialization")
        .Produces<CreateSpecializationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Create a specialization")
        .RequireAuthorization("Admin");

        specializations.MapPut("/{id:guid}", async (Guid id, UpdateSpecializationRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateSpecializationCommand(
                id,
                request.Name,
                request.Description,
                request.Icon,
                request.IsActive));
            return ToApiResult(result);
        })
        .WithName("UpdateSpecialization")
        .Produces<UpdateSpecializationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a specialization")
        .RequireAuthorization("Admin");
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    private static IResult ToApiResult(Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    private static IResult ToProblem(
        Outcome outcome,
        IReadOnlyList<Error> errors,
        IReadOnlyList<string> messages)
    {
        // Prefer structured Error record (Code + Message) — handlers MUST use Error records.
        // Messages fallback handles any remaining legacy message-only returns.
        if (errors.Count > 0)
        {
            var first = errors[0];
            return Results.Problem(
                statusCode: (int)outcome,
                title: first.Code,
                detail: first.Message);
        }

        return Results.Problem(
            statusCode: (int)outcome,
            detail: messages.Count > 0 ? messages[0] : null);
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
    string? Slug = null,
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

public sealed record CreateTagRequest(string Name, string Slug);
public sealed record UpdateTagRequest(string Name, string Slug);
public sealed record AssignCategoriesToEntityRequest(string EntityType, Guid EntityId, IReadOnlyList<Guid> CategoryIds);
public sealed record AssignTagsToEntityRequest(string EntityType, Guid EntityId, IReadOnlyList<Guid> TagIds);

public sealed record UploadAttachmentRequest(
    string EntityType,
    Guid EntityId,
    string AttachmentType,
    int? Width = null,
    int? Height = null,
    int? DurationSeconds = null,
    int SortOrder = 0);

public sealed record ReorderAttachmentsRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> OrderedAttachmentIds);

public sealed record SetPrimaryImageRequest(
    string EntityType,
    Guid EntityId,
    Guid AttachmentId);

public sealed record ReorderCategoryItem(Guid CategoryId, int SortOrder);
public sealed record ReorderCategoriesRequest(IReadOnlyList<ReorderCategoryItem> Items);

public sealed record CreateSpecializationRequest(string Name, string? Description = null, string? Icon = null);
public sealed record UpdateSpecializationRequest(string Name, string? Description = null, string? Icon = null, bool? IsActive = null);
