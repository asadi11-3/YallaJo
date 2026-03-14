using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;

public sealed class RemoveCategoryFromEntityCommandHandler(
    IEntityCategoryRepository entityCategoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<RemoveCategoryFromEntityCommand>
{
    public async Task<Result> Handle(RemoveCategoryFromEntityCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            return Result.Invalid(new Error("InvalidEntityType", "Invalid entity type."));

        var entityCategories = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, ct);
        var entityCategory = entityCategories.FirstOrDefault(ec => ec.CategoryId == request.CategoryId);
        if (entityCategory is null)
            return Result.NotFound(
                $"Category '{request.CategoryId}' is not assigned to {request.EntityType}/{request.EntityId}.");

        entityCategoryRepository.Remove(entityCategory);
        await unitOfWork.SaveChangesAsync(ct);
        cache.Remove(ContentCoreCacheKeys.EntityCategories(request.EntityType, request.EntityId));

        return Result.Success();
    }
}
