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

namespace ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;

public sealed class RemoveCategoryFromEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IContentCoreOutboxWriter outboxWriter,
    HybridCache cache,
    IOwnershipGuard ownershipGuard,
    ILogger<RemoveCategoryFromEntityCommandHandler> logger)
    : ICommandHandler<RemoveCategoryFromEntityCommand>
{
    public async Task<Result> Handle(RemoveCategoryFromEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. EntityType parse / validation
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            {
                return Result.Failure(
                    new Error("EntityCategory.InvalidEntityType", "Invalid entity type."),
                    Outcome.Invalid);
            }

            // 2. Ownership guard (admin-tier bypass + ownership check)
            var authResult = await ownershipGuard.AuthorizeAsync(
                entityType, request.EntityId, "EntityCategory",
                "You do not have permission to remove categories from this entity.",
                cancellationToken);

            if (!authResult.IsSuccess)
                return authResult;

            var entityCategories = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var entityCategory = entityCategories.FirstOrDefault(ec => ec.CategoryId == request.CategoryId);
            if (entityCategory is null) {
                return Result.Failure(
                    new Error(
                        "EntityCategory.NotFound",
                        $"Category '{request.CategoryId}' is not assigned to {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);
            }
            entityCategoryRepository.Remove(entityCategory);
            outboxWriter.Enqueue(new EntityCategoryRemovedIntegrationEvent(
                request.EntityType,
                request.EntityId,
                request.CategoryId,
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
                ContentCoreCacheKeys.EntityCategoriesTag(request.EntityType, request.EntityId),
                cancellationToken);

            logger.LogInformation(
                "Removed category {CategoryId} from {EntityType}/{EntityId}",
                request.CategoryId, request.EntityType, request.EntityId);

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
