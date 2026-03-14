using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (category is null)
            return Result.NotFound($"Category '{request.Id}' not found.");

        category.SoftDelete();
        await unitOfWork.SaveChangesAsync(ct);

        InvalidateCategoryCache(cache, request.Id);
        return Result.Success();
    }

    internal static void InvalidateCategoryCache(IMemoryCache cache, Guid id)
    {
        // Invalidate all root-level list permutations
        foreach (var key in ContentCoreCacheKeys.CommonCategoryListKeys())
            cache.Remove(key);

        // Invalidate both translation variants for this specific category
        cache.Remove(ContentCoreCacheKeys.Category(id, false));
        cache.Remove(ContentCoreCacheKeys.Category(id, true));
    }
}
