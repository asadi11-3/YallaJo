using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Attachment.GetAttachmentById;

public sealed class GetAttachmentByIdQueryHandler(
    IAttachmentRepository attachmentRepository,
    ILogger<GetAttachmentByIdQueryHandler> logger)
    : IQueryHandler<GetAttachmentByIdQuery, AttachmentDto>
{
    public async Task<Result<AttachmentDto>> Handle(
        GetAttachmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await attachmentRepository.GetByIdAsync(
                request.AttachmentId, cancellationToken, asNoTracking: true);

            if (attachment is null)
            {
                return Result<AttachmentDto>.NotFound(
                    $"Attachment '{request.AttachmentId}' not found.");
            }

            logger.LogDebug("GetAttachmentById: {AttachmentId} found", request.AttachmentId);

            return Result<AttachmentDto>.Success(new AttachmentDto(
                attachment.Id,
                attachment.EntityType,
                attachment.EntityId,
                attachment.Type,
                attachment.Url,
                attachment.ThumbnailUrl,
                attachment.OriginalFileName,
                attachment.MimeType,
                attachment.FileSize,
                attachment.Width,
                attachment.Height,
                attachment.DurationSeconds,
                attachment.SortOrder,
                attachment.UploadedAt,
                attachment.UploadedByUserId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<AttachmentDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
