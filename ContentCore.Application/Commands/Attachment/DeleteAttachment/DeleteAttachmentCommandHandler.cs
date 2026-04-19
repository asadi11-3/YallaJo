using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
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

            // IDOR: only the uploader or an admin may delete an attachment
            var isAdmin = currentUser.IsInRole("Admin");
            if (!isAdmin && attachment.UploadedByUserId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to delete this attachment."),
                    Outcome.Forbidden);
            }

            // Capture file URL before removing from DB
            var fileUrl = attachment.Url;
            var entityType = attachment.EntityType;
            var entityId = attachment.EntityId;

            // Remove DB record — no MarkForDeletion() here.
            // Physical file deletion happens AFTER a successful commit to avoid
            // losing a file whose DB record was never actually removed (on save failure).
            attachmentRepository.Remove(attachment);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
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
            await cache.RemoveByTagAsync($"attachments:{entityType}:{entityId}", cancellationToken);
            await cache.RemoveByTagAsync($"attachment:{request.AttachmentId}", cancellationToken);

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
