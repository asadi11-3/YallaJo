using ContentCore.Application.Authorization;
using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Attachment.GetAttachmentById;

public sealed class GetAttachmentByIdQueryHandler(
    IAttachmentRepository attachmentRepository,
    IOwnershipGuard ownershipGuard,
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

            // Read-side IDOR guard (Patch 1B): admin-tier bypass + ownership check on the
            // owning entity. Any denial (Forbidden / unsupported / deleted / target-not-found)
            // is mapped to NotFound so a non-owner cannot distinguish "exists but forbidden"
            // from "does not exist" — prevents attachment existence enumeration by GUID.
            var authResult = await ownershipGuard.AuthorizeAsync(
                attachment.EntityType, attachment.EntityId, "Attachment",
                ct: cancellationToken);

            if (!authResult.IsSuccess)
            {
                logger.LogInformation(
                    "GetAttachmentById: access denied for {AttachmentId} (returned as NotFound)",
                    request.AttachmentId);

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
