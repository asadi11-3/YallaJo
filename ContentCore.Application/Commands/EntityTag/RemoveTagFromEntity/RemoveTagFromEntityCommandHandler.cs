using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Application.Interfaces;
using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed class RemoveTagFromEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    IContentCoreUnitOfWork unitOfWork,
    IContentCoreOutboxWriter outboxWriter,
    HybridCache cache,
    IOwnershipGuard ownershipGuard,
    ILogger<RemoveTagFromEntityCommandHandler> logger)
    : ICommandHandler<RemoveTagFromEntityCommand>
{
    public async Task<Result> Handle(RemoveTagFromEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. EntityType parse / validation
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            {
                return Result.Failure(
                    new Error("EntityTag.InvalidEntityType", "Invalid entity type."),
                    Outcome.Invalid);
            }

            // 2. Ownership guard (admin-tier bypass + ownership check)
            var authResult = await ownershipGuard.AuthorizeAsync(
                entityType, request.EntityId, "EntityTag",
                "You do not have permission to remove tags from this entity.",
                cancellationToken);

            if (!authResult.IsSuccess)
                return authResult;

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
            outboxWriter.Enqueue(new EntityTagRemovedIntegrationEvent(
                request.EntityType,
                request.EntityId,
                request.TagId,
                DateTime.UtcNow));

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
