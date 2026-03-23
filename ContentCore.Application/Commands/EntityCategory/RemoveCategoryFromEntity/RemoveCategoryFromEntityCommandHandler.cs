using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;

public sealed class RemoveCategoryFromEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<RemoveCategoryFromEntityCommand>
{
    public async Task<Result> Handle(RemoveCategoryFromEntityCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result.Failure(
                    new Error("Entity.InvalidType", $"Entity type '{request.EntityType}' is not recognized."),
                    Outcome.Invalid);

            var entityCategories = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, ct);
            var entityCategory = entityCategories.FirstOrDefault(ec => ec.CategoryId == request.CategoryId);
            if (entityCategory is null)
                return Result.Failure(
                    new Error("EntityCategory.NotFound",
                        $"Category '{request.CategoryId}' is not assigned to {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);

            entityCategoryRepository.Remove(entityCategory);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Entity.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"entity-categories:{request.EntityType}:{request.EntityId}", ct);

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
