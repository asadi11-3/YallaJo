using ContentCore.Domain.Enums;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;

public sealed class AssignCategoriesToEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AssignCategoriesToEntityCommandHandler> logger)
    : ICommandHandler<AssignCategoriesToEntityCommand>
{
    public async Task<Result> Handle(AssignCategoriesToEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entityType = Enum.Parse<EntityType>(request.EntityType, true);

            var categories = await categoryRepository.GetAllAsync(
                filter: c => request.CategoryIds.Contains(c.Id),
                ct: cancellationToken);

            if (categories.Count != request.CategoryIds.Count)
            {
                var foundIds = categories.Select(c => c.Id).ToHashSet();
                var missingId = request.CategoryIds.First(id => !foundIds.Contains(id));
                return Result.Failure(
                    new Error("Category.NotFound", $"Category '{missingId}' was not found."),
                    Outcome.NotFound);
            }

            var inactiveCategory = categories.FirstOrDefault(c => !c.IsActive);
            if (inactiveCategory is not null) {
                return Result.Failure(
                    new Error(
                        "Category.Inactive",
                        $"Category '{inactiveCategory.Id}' is inactive and cannot be assigned."),
                    Outcome.Invalid);
            }

            var existing = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var existingIds = existing.Select(x => x.CategoryId).ToHashSet();

            foreach (var categoryId in request.CategoryIds)
            {
                if (existingIds.Contains(categoryId))
                    continue;

                var entityCategory = ContentCore.Domain.Entities.EntityCategory.Create(entityType, request.EntityId, categoryId);
                entityCategoryRepository.Add(entityCategory);
                existingIds.Add(categoryId);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Entity.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"entity-categories:{request.EntityType}:{request.EntityId}", cancellationToken);

            logger.LogInformation(
                "Assigned {Count} categories to {EntityType}/{EntityId}",
                request.CategoryIds.Count, request.EntityType, request.EntityId);

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
