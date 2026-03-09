using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandHandler(
    IFileStorageService fileStorageService,
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMediaProcessingQueue mediaProcessingQueue)
    : ICommandHandler<UploadAttachmentCommand, UploadAttachmentResult>
{
    public async Task<Result<UploadAttachmentResult>> Handle(
        UploadAttachmentCommand request,
        CancellationToken ct)
    {
        // 1. Upload file to storage
        var folder = request.EntityType.ToString().ToLowerInvariant() + "s";
        var uploadResult = await fileStorageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            folder,
            ct);

        // 2. Create attachment entity (domain event raised inside Create())
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

        // 4. Persist (domain events fire here — creates EntityImage for images)
        await attachmentRepository.AddAsync(attachment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // 5. Enqueue background media processing (thumbnails, metadata extraction)
        //    Enqueued AFTER save to guarantee the attachment is persisted before processing.
        await mediaProcessingQueue.EnqueueAsync(
            new MediaProcessingJob(attachment.Id, attachment.Url, request.Type), ct);

        return Result<UploadAttachmentResult>.Created(
            new UploadAttachmentResult(attachment.Id, attachment.Url, uploadResult.FileSize));
    }
}