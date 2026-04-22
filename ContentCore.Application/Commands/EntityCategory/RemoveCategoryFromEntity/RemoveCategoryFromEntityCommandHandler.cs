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
    HybridCache cache,
    ILogger<RemoveCategoryFromEntityCommandHandler> logger)
    : ICommandHandler<RemoveCategoryFromEntityCommand>
{
    public async Task<Result> Handle(RemoveCategoryFromEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entityType = Enum.Parse<EntityType>(request.EntityType, true);

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

            await cache.RemoveByTagAsync($"entity-categories:{request.EntityType}:{request.EntityId}", cancellationToken);

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
