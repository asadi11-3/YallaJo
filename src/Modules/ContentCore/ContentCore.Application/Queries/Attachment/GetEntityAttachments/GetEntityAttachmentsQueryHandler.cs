using ContentCore.Application.Authorization;
using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

public sealed class GetEntityAttachmentsQueryHandler(
    IAttachmentRepository attachmentRepository,
    IOwnershipGuard ownershipGuard,
    ILogger<GetEntityAttachmentsQueryHandler> logger)
    : IQueryHandler<GetEntityAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<Result<IReadOnlyList<AttachmentDto>>> Handle(
        GetEntityAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Read-side IDOR guard (Patch 1B): admin-tier bypass + ownership check on the
            // target entity BEFORE listing its attachments. Any denial is mapped to NotFound
            // so a non-owner cannot enumerate which entities have attachments. Public
            // published tour/blog images are served by a separate path (PublicEntityImageReader)
            // and are unaffected by this owner/admin-scoped management endpoint.
            var authResult = await ownershipGuard.AuthorizeAsync(
                request.EntityType, request.EntityId, "Attachment",
                ct: cancellationToken);

            if (!authResult.IsSuccess)
            {
                logger.LogInformation(
                    "GetEntityAttachments: access denied for {EntityType}/{EntityId} (returned as NotFound)",
                    request.EntityType, request.EntityId);

                return Result<IReadOnlyList<AttachmentDto>>.NotFound(
                    $"No attachments found for the specified entity.");
            }

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
