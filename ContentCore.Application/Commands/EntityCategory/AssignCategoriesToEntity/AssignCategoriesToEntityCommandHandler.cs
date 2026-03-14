using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;

public sealed class AssignCategoriesToEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<AssignCategoriesToEntityCommand>
{
    public async Task<Result> Handle(AssignCategoriesToEntityCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            return Result.Invalid(new Error("InvalidEntityType", "Invalid entity type."));

        foreach (var categoryId in request.CategoryIds)
        {
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is null)
                return Result.NotFound($"Category '{categoryId}' not found.");

            var exists = await entityCategoryRepository.ExistsAsync(entityType, request.EntityId, categoryId, ct);
            if (exists)
                continue;

            var entityCategory = ContentCore.Domain.Entities.EntityCategory.Create(entityType, request.EntityId, categoryId);
            entityCategoryRepository.Add(entityCategory);
        }

        await unitOfWork.SaveChangesAsync(ct);
        cache.Remove(ContentCoreCacheKeys.EntityCategories(request.EntityType, request.EntityId));
        return Result.Success();
    }
}
