using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed class ReorderAttachmentsCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IOwnershipGuard ownershipGuard,
    HybridCache cache,
    ILogger<ReorderAttachmentsCommandHandler> logger)
    : ICommandHandler<ReorderAttachmentsCommand>
{
    public async Task<Result> Handle(ReorderAttachmentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var attachments = await attachmentRepository.GetAllAsync(
                filter: x => x.EntityType == request.EntityType && x.EntityId == request.EntityId,
                asNoTracking: false,
                ct: cancellationToken);

            if (attachments.Count == 0)
            {
                return Result.Failure(
                    new Error(
                        "Attachment.NotFound",
                        $"No attachments found for {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);
            }

            // Ownership guard (admin-tier bypass + ownership check)
            var authResult = await ownershipGuard.AuthorizeAsync(
                request.EntityType, request.EntityId, "Attachment",
                "You do not have permission to reorder attachments for this entity.",
                cancellationToken);

            if (!authResult.IsSuccess)
                return authResult;

            var attachmentMap = attachments.ToDictionary(a => a.Id);

            for (var i = 0; i < request.OrderedAttachmentIds.Count; i++)
            {
                if (!attachmentMap.TryGetValue(request.OrderedAttachmentIds[i], out var attachment))
                {
                    return Result.Failure(
                        new Error(
                            "Attachment.NotFound",
                            $"Attachment '{request.OrderedAttachmentIds[i]}' does not belong to this entity."),
                        Outcome.NotFound);
                }

                attachment.SetSortOrder(i);
            }

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

            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.EntityAttachmentsTag(request.EntityType.ToString(), request.EntityId),
                cancellationToken);

            logger.LogInformation(
                "Reordered {Count} attachments for {EntityType}/{EntityId}",
                request.OrderedAttachmentIds.Count, request.EntityType, request.EntityId);

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
