using ContentCore.Application.Interfaces;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandHandler(
    IFileStorageService fileStorageService,
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMediaProcessingQueue mediaProcessingQueue,
    HybridCache cache)
    : ICommandHandler<UploadAttachmentCommand, UploadAttachmentResult>
{
    public async Task<Result<UploadAttachmentResult>> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Upload file to storage
            var folder = request.EntityType.ToString().ToLowerInvariant() + "s";
            var uploadResult = await fileStorageService.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                folder,
                cancellationToken);

            var attachment = Domain.Entities.Attachment.Create(
                request.EntityType,
                request.EntityId,
                request.Type,
                uploadResult.Url,
                request.UploadedByUserId,
                request.FileName,
                request.ContentType,
                uploadResult.FileSize,
                request.SortOrder);

            // 3. Set optional media metadata
            if (request.Width.HasValue && request.Height.HasValue)
                attachment.SetDimensions(request.Width.Value, request.Height.Value);

            if (request.DurationSeconds.HasValue)
                attachment.SetDuration(request.DurationSeconds.Value);

            await attachmentRepository.AddAsync(attachment, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result<UploadAttachmentResult>.Conflict(
                    new Error(
                        "Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync($"attachments:{request.EntityType}:{request.EntityId}", cancellationToken);

            // 5. Enqueue background media processing (thumbnails, metadata extraction)
            //    Enqueued AFTER save to guarantee the attachment is persisted before processing.
            await mediaProcessingQueue.EnqueueAsync(
                new MediaProcessingJob(attachment.Id, attachment.Url, request.Type), cancellationToken);

            return Result<UploadAttachmentResult>.Created(
                new UploadAttachmentResult(attachment.Id, attachment.Url, uploadResult.FileSize));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UploadAttachmentResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
