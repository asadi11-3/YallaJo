using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

public sealed class GetEntityAttachmentsQueryHandler(
    IAttachmentRepository attachmentRepository,
    ILogger<GetEntityAttachmentsQueryHandler> logger)
    : IQueryHandler<GetEntityAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<Result<IReadOnlyList<AttachmentDto>>> Handle(
        GetEntityAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var attachments = await attachmentRepository.GetAllAsync(
                filter: x => x.EntityType == request.EntityType && x.EntityId == request.EntityId,
                orderBy: q => q.OrderBy(x => x.SortOrder),
                ct: cancellationToken);

            // Load the EntityImage join to surface primary-image state (Gap 2). The
            // primary flag lives on EntityImage, not Attachment, so we build a set of
            // primary AttachmentIds and project it onto each DTO.
            var entityImages = await attachmentRepository.GetEntityImagesAsync(
                request.EntityType, request.EntityId, cancellationToken);
            var primaryAttachmentIds = entityImages
                .Where(ei => ei.IsPrimary)
                .Select(ei => ei.AttachmentId)
                .ToHashSet();

            var dtos = attachments.Select(a => new AttachmentDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Type,
                a.Url,
                a.ThumbnailUrl,
                a.OriginalFileName,
                a.MimeType,
                a.FileSize,
                a.Width,
                a.Height,
                a.DurationSeconds,
                a.SortOrder,
                a.UploadedAt,
                a.UploadedByUserId,
                IsPrimary: primaryAttachmentIds.Contains(a.Id))).ToList();

            logger.LogDebug(
                "GetEntityAttachments: {Count} attachments for {EntityType}/{EntityId}",
                dtos.Count, request.EntityType, request.EntityId);

            return Result<IReadOnlyList<AttachmentDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<AttachmentDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
