using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    IContentCoreUnitOfWork unitOfWork,
    IOwnershipGuard ownershipGuard,
    HybridCache cache,
    ILogger<DeleteAttachmentCommandHandler> logger)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await attachmentRepository.GetByIdAsync(
                request.AttachmentId, cancellationToken, asNoTracking: false);

            if (attachment is null)
            {
                return Result.Failure(
                    new Error("Attachment.NotFound", $"Attachment '{request.AttachmentId}' was not found."),
                    Outcome.NotFound);
            }

            // Ownership guard (admin-tier bypass + ownership check)
            var authResult = await ownershipGuard.AuthorizeAsync(
                attachment.EntityType, attachment.EntityId, "Attachment",
                "You do not have permission to delete this attachment.",
                cancellationToken);

            if (!authResult.IsSuccess)
                return authResult;

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
