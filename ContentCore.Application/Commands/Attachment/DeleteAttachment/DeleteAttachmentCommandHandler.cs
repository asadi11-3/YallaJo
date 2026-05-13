using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    IContentCoreUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver,
    HybridCache cache,
    ILogger<DeleteAttachmentCommandHandler> logger)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

            var attachment = await attachmentRepository.GetByIdAsync(
                request.AttachmentId, cancellationToken, asNoTracking: false);

            if (attachment is null)
            {
                return Result.Failure(
                    new Error("Attachment.NotFound", $"Attachment '{request.AttachmentId}' was not found."),
                    Outcome.NotFound);
            }

            // IDOR: only the target-entity owner or an admin-tier role (Admin/SuperAdmin/Owner)
            // may delete an attachment. Uploader alone is not sufficient — uploading does not
            // grant control over the surrounding entity (Tour/Place/Business/Blog/Review/TourGuide).
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            if (!isAdminTier)
            {
                var ownership = await ownershipResolver.ResolveAsync(
                    attachment.EntityType, attachment.EntityId, cancellationToken);

                if (!ownership.IsSupported)
                {
                    return Result.Failure(
                        new Error(
                            "Attachment.UnsupportedEntityType",
                            "This entity type cannot be authorized for attachment operations."),
                        Outcome.Invalid);
                }

                if (!ownership.Exists)
                {
                    return Result.Failure(
                        new Error(
                            "Attachment.TargetNotFound",
                            $"{attachment.EntityType} '{attachment.EntityId}' was not found."),
                        Outcome.NotFound);
                }

                if (ownership.IsDeleted)
                {
                    return Result.Failure(
                        new Error(
                            "Attachment.TargetDeleted",
                            $"{attachment.EntityType} '{attachment.EntityId}' is deleted."),
                        Outcome.Invalid);
                }

                if (ownership.OwnerUserId != currentUser.UserId.Value)
                {
                    return Result.Failure(
                        Error.Forbidden("You do not have permission to delete this attachment."),
                        Outcome.Forbidden);
                }
            }

            // Capture file URL before removing from DB
            var fileUrl = attachment.Url;
            var entityType = attachment.EntityType;
            var entityId = attachment.EntityId;

            // Raise the domain event before removing the row so the outbox writer
            // (AttachmentDeletedDomainEventHandler) commits the integration event
            // atomically with the row deletion via the existing UnitOfWork.
            // Physical file deletion still happens AFTER a successful commit to avoid
            // losing a file whose DB record was never actually removed (on save failure).
            attachment.MarkForDeletion();
            attachmentRepository.Remove(attachment);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // DB record is confirmed deleted — now safe to delete the physical file.
            // If file deletion fails we log it for manual cleanup but don't fail the request:
            // the DB record is gone so the file is now truly orphaned (no live references).
            var deleted = await fileStorageService.DeleteAsync(fileUrl, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning(
                    "Attachment DB record deleted but physical file not found: {FileUrl} " +
                    "(AttachmentId={AttachmentId}). File may need manual cleanup.",
                    fileUrl, request.AttachmentId);
            }

            // Fine-grained eviction: only invalidate this entity's attachment list + this specific attachment.
            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.EntityAttachmentsTag(entityType.ToString(), entityId),
                cancellationToken);
            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.AttachmentTag(request.AttachmentId),
                cancellationToken);

            logger.LogInformation(
                "Attachment deleted: {AttachmentId} (EntityType={EntityType}, EntityId={EntityId})",
                request.AttachmentId, entityType, entityId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
