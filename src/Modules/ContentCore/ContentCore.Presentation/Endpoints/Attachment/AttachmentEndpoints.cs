using ContentCore.Application.Commands.Attachment.DeleteAttachment;
using ContentCore.Application.Commands.Attachment.ReorderAttachments;
using ContentCore.Application.Commands.Attachment.SetPrimaryImage;
using ContentCore.Application.Commands.Attachment.UploadAttachment;
using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Application.Queries.Attachment.GetAttachmentById;
using ContentCore.Application.Queries.Attachment.GetEntityAttachments;
using ContentCore.Domain.Enums;
using ContentCore.Presentation.Endpoints.Attachment.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Attachment;

internal static class AttachmentEndpoints
{
    internal static void MapAttachmentEndpoints(RouteGroupBuilder group)
    {
        var attachments = group.MapGroup("/attachments").WithTags("ContentCore | Attachments");

        // Upload attachment (multipart/form-data)
        attachments.MapPost("/", async (IFormFile file, [AsParameters] UploadAttachmentRequest request,
            ICurrentUser currentUser, ISender sender, CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            if (!Enum.TryParse<Domain.Enums.AttachmentType>(request.AttachmentType, true, out var attachmentType))
                return Results.BadRequest("Invalid AttachmentType.");
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            await using var stream = file.OpenReadStream();
            var result = await sender.Send(
                new UploadAttachmentCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                entityType,
                request.EntityId,
                attachmentType,
                currentUser.UserId.Value,
                request.Width,
                request.Height,
                request.DurationSeconds,
                request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("UploadAttachment")
        .Produces<UploadAttachmentResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Upload a file attachment for an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Create))
        .RequireAuthorization()
        .DisableAntiforgery();

        // Get attachments by entity
        attachments.MapGet("/", async (EntityType entityType, Guid entityId, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetEntityAttachmentsQuery(entityType, entityId), ct);
            return result.ToApiResult();
        })
        .WithName("GetEntityAttachments")
        .Produces<IReadOnlyList<AttachmentDto>>(StatusCodes.Status200OK)
        .WithSummary("List attachments for an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Read))
        .RequireAuthorization();

        // Get attachment by ID
        attachments.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetAttachmentByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetAttachmentById")
        .Produces<AttachmentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a single attachment by ID")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Read))
        .RequireAuthorization();

        // Delete attachment
        attachments.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new DeleteAttachmentCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteAttachment")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete an attachment and its file")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Delete))
        .RequireAuthorization();

        // Reorder attachments
        attachments.MapPut("/reorder", async (ReorderAttachmentsRequest request, ISender sender, CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(
                new ReorderAttachmentsCommand(
                entityType, request.EntityId, request.OrderedAttachmentIds), ct);
            return result.ToApiResult();
        })
        .WithName("ReorderAttachments")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder attachments for an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Update))
        .RequireAuthorization();

        // Set primary image
        attachments.MapPut("/primary", async (SetPrimaryImageRequest request, ISender sender, CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(
                new SetPrimaryImageCommand(
                entityType, request.EntityId, request.AttachmentId), ct);
            return result.ToApiResult();
        })
        .WithName("SetPrimaryImage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Set an attachment as the primary image for an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.EntityImage, AppAction.Update))
        .RequireAuthorization();

        // Bulk upload images (up to 20 images, multipart/form-data)
        // POST /attachments/images — entityType + entityId as query params
        attachments.MapPost("/images", async (
            IFormFileCollection files,
            string entityType,
            Guid entityId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct = default) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
                return Results.BadRequest("Invalid entityType.");

            if (files.Count == 0)
                return Results.BadRequest("At least one file is required.");

            if (files.Count > 20)
                return Results.BadRequest("A maximum of 20 images can be uploaded at once.");

            var uploadedIds = new List<Guid>(files.Count);
            var errors = new List<string>();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                await using var stream = file.OpenReadStream();
                var result = await sender.Send(
                    new UploadAttachmentCommand(
                        stream,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        parsedEntityType,
                        entityId,
                        AttachmentType.Image,
                        currentUser.UserId.Value,
                        SortOrder: i),
                    ct);

                if (result.IsSuccess)
                    uploadedIds.Add(result.Value.Id);
                else
                    errors.Add($"{file.FileName}: {result.Error?.Message ?? "Upload failed"}");
            }

            if (uploadedIds.Count == 0)
                return Results.UnprocessableEntity(new { errors });

            return Results.Ok(new BulkUploadImagesResult(uploadedIds, errors));
        })
        .WithName("BulkUploadImages")
        .Produces<BulkUploadImagesResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Bulk upload up to 20 images for an entity")
        .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Create))
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}

// ── Response Model ────────────────────────────────────────────────────────────

public sealed record BulkUploadImagesResult(
    IReadOnlyList<Guid> UploadedAttachmentIds,
    IReadOnlyList<string> Errors);
