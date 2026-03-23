using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;

public sealed class AssignCategoriesToEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<AssignCategoriesToEntityCommand>
{
    public async Task<Result> Handle(AssignCategoriesToEntityCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result.Failure(
                    new Error("Entity.InvalidType", $"Entity type '{request.EntityType}' is not recognized."),
                    Outcome.Invalid);

            foreach (var categoryId in request.CategoryIds)
            {
                var category = await categoryRepository.GetByIdAsync(categoryId, ct);
                if (category is null)
                    return Result.Failure(
                        new Error("Category.NotFound", $"Category '{categoryId}' was not found."),
                        Outcome.NotFound);

                var exists = await entityCategoryRepository.ExistsAsync(entityType, request.EntityId, categoryId, ct);
                if (exists)
                    continue;

                var entityCategory = ContentCore.Domain.Entities.EntityCategory.Create(entityType, request.EntityId, categoryId);
                entityCategoryRepository.Add(entityCategory);
            }

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
