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
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Presentation;

namespace ContentCore.Presentation.Endpoints.Attachment;

internal static class AttachmentEndpoints
{
    internal static void MapAttachmentEndpoints(RouteGroupBuilder group)
    {
        var attachments = group.MapGroup("/attachments").WithTags("ContentCore | Attachments");

        // Upload attachment (multipart/form-data)
        attachments.MapPost("/", async (IFormFile file, [AsParameters] UploadAttachmentRequest request, ICurrentUser currentUser, ISender sender) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            if (!Enum.TryParse<Domain.Enums.AttachmentType>(request.AttachmentType, true, out var attachmentType))
                return Results.BadRequest("Invalid AttachmentType.");
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            await using var stream = file.OpenReadStream();
            var result = await sender.Send(new UploadAttachmentCommand(
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
                request.SortOrder));
            return result.ToApiResult();
        })
        .WithName("UploadAttachment")
        .Produces<UploadAttachmentResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithSummary("Upload a file attachment for an entity")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Attachment, AppAction.Create))
        .RequireAuthorization()
        .DisableAntiforgery();

        // Get attachments by entity
        attachments.MapGet("/", async (EntityType entityType, Guid entityId, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityAttachmentsQuery(entityType, entityId));
            return result.ToApiResult();
        })
        .WithName("GetEntityAttachments")
        .Produces<IReadOnlyList<AttachmentDto>>(StatusCodes.Status200OK)
        .WithSummary("List attachments for an entity")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Attachment, AppAction.Read))
        .RequireAuthorization();

        // Get attachment by ID
        attachments.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetAttachmentByIdQuery(id));
            return result.ToApiResult();
        })
        .WithName("GetAttachmentById")
        .Produces<AttachmentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a single attachment by ID")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Attachment, AppAction.Read))
        .RequireAuthorization();

        // Delete attachment
        attachments.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteAttachmentCommand(id));
            return result.ToApiResult();
        })
        .WithName("DeleteAttachment")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete an attachment and its file")
        .RequireAuthorization("Permission.Attachment.Delete");

        // Reorder attachments
        attachments.MapPut("/reorder", async (ReorderAttachmentsRequest request, ISender sender) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(new ReorderAttachmentsCommand(
                entityType, request.EntityId, request.OrderedAttachmentIds));
            return result.ToApiResult();
        })
        .WithName("ReorderAttachments")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reorder attachments for an entity")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Attachment, AppAction.Update))
        .RequireAuthorization();

        // Set primary image
        attachments.MapPut("/primary", async (SetPrimaryImageRequest request, ISender sender) =>
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Results.BadRequest("Invalid EntityType.");

            var result = await sender.Send(new SetPrimaryImageCommand(
                entityType, request.EntityId, request.AttachmentId));
            return result.ToApiResult();
        })
        .WithName("SetPrimaryImage")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Set an attachment as the primary image for an entity")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.EntityImage, AppAction.Update))
        .RequireAuthorization();
    }
}
