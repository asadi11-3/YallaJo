using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed class ReorderAttachmentsCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver,
    HybridCache cache,
    ILogger<ReorderAttachmentsCommandHandler> logger)
    : ICommandHandler<ReorderAttachmentsCommand>
{
    public async Task<Result> Handle(ReorderAttachmentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

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

            // IDOR: only the target-entity owner or an admin-tier role (Admin/SuperAdmin/Owner)
            // may reorder. Reordering mutates the target entity's gallery presentation, so
            // authorization belongs to that entity's owner — not to anyone who uploaded a single
            // attachment in the set.
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            if (!isAdminTier)
            {
                var ownership = await ownershipResolver.ResolveAsync(
                    request.EntityType, request.EntityId, cancellationToken);

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
                            $"{request.EntityType} '{request.EntityId}' was not found."),
                        Outcome.NotFound);
                }

                if (ownership.IsDeleted)
                {
                    return Result.Failure(
                        new Error(
                            "Attachment.TargetDeleted",
                            $"{request.EntityType} '{request.EntityId}' is deleted."),
                        Outcome.Invalid);
                }

                if (ownership.OwnerUserId != currentUser.UserId.Value)
                {
                    return Result.Failure(
                        Error.Forbidden("You do not have permission to reorder attachments for this entity."),
                        Outcome.Forbidden);
                }
            }

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
