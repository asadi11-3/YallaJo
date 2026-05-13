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

namespace ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;

public sealed class AssignTagsToEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver,
    ILogger<AssignTagsToEntityCommandHandler> logger)
    : ICommandHandler<AssignTagsToEntityCommand>
{
    public async Task<Result> Handle(AssignTagsToEntityCommand request, CancellationToken cancellationToken)
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
                        Error.Forbidden("You do not have permission to assign tags to this entity."),
                        Outcome.Forbidden);
                }
            }

            var tags = await tagRepository.GetAllAsync(
                filter: t => request.TagIds.Contains(t.Id), ct: cancellationToken);

            if (tags.Count != request.TagIds.Count)
            {
                var foundIds = tags.Select(t => t.Id).ToHashSet();
                var missingId = request.TagIds.First(id => !foundIds.Contains(id));
                return Result.Failure(
                    new Error("Tag.NotFound", $"Tag '{missingId}' was not found."),
                    Outcome.NotFound);
            }

            var inactiveTag = tags.FirstOrDefault(t => !t.IsActive);
            if (inactiveTag is not null)
            {
                return Result.Failure(
                    new Error(
                        "Tag.Inactive",
                        $"Tag '{inactiveTag.Id}' is inactive and cannot be assigned."),
                    Outcome.Invalid);
            }

            var existing = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var existingIds = existing.Select(x => x.TagId).ToHashSet();

            foreach (var tagId in request.TagIds)
            {
                if (existingIds.Contains(tagId))
                    continue;

                var entityTag = ContentCore.Domain.Entities.EntityTag.Create(entityType, request.EntityId, tagId);
                entityTagRepository.Add(entityTag);
                existingIds.Add(tagId);
            }

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
                "Assigned {Count} tags to {EntityType}/{EntityId}",
                request.TagIds.Count, request.EntityType, request.EntityId);

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
