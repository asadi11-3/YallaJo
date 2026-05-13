using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed class RemoveTagFromEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver,
    ILogger<RemoveTagFromEntityCommandHandler> logger)
    : ICommandHandler<RemoveTagFromEntityCommand>
{
    public async Task<Result> Handle(RemoveTagFromEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Authentication
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            // 2. EntityType parse / validation
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            {
                return Result.Failure(
                    new Error("EntityTag.InvalidEntityType", "Invalid entity type."),
                    Outcome.Invalid);
            }

            // 3. Admin-tier short-circuit
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            // 4. Ownership probe (skipped for admin-tier callers)
            if (!isAdminTier)
            {
                var ownership = await ownershipResolver.ResolveAsync(entityType, request.EntityId, cancellationToken);

                if (!ownership.IsSupported)
                {
                    return Result.Failure(
                        new Error(
                            "EntityTag.UnsupportedEntityType",
                            "This entity type cannot be authorized for assignment."),
                        Outcome.Invalid);
                }

                if (!ownership.Exists)
                {
                    return Result.Failure(
                        new Error(
                            "EntityTag.TargetNotFound",
                            $"{entityType} '{request.EntityId}' was not found."),
                        Outcome.NotFound);
                }

                if (ownership.IsDeleted)
                {
                    return Result.Failure(
                        new Error(
                            "EntityTag.TargetDeleted",
                            $"{entityType} '{request.EntityId}' is deleted."),
                        Outcome.Invalid);
                }

                if (ownership.OwnerUserId != currentUser.UserId.Value)
                {
                    return Result.Failure(
                        Error.Forbidden("You do not have permission to remove tags from this entity."),
                        Outcome.Forbidden);
                }
            }

            var entityTags = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var entityTag = entityTags.FirstOrDefault(et => et.TagId == request.TagId);
            if (entityTag is null) {
                return Result.Failure(
                      new Error(
                          "EntityTag.NotFound",
                          $"Tag '{request.TagId}' is not assigned to {request.EntityType}/{request.EntityId}."),
                      Outcome.NotFound);
            }

            entityTagRepository.Remove(entityTag);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Entity.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.EntityTagsTag(request.EntityType, request.EntityId),
                cancellationToken);

            logger.LogInformation(
                "Removed tag {TagId} from {EntityType}/{EntityId}",
                request.TagId, request.EntityType, request.EntityId);

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
