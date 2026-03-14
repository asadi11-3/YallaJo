using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<DeactivateCategoryCommand, DeactivateCategoryResult>
{
    public async Task<Result<DeactivateCategoryResult>> Handle(
        DeactivateCategoryCommand request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (category is null)
            return Result<DeactivateCategoryResult>.NotFound(
                $"Category '{request.Id}' not found.");

        category.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        DeleteCategoryCommandHandler.InvalidateCategoryCache(cache, request.Id);

        return Result<DeactivateCategoryResult>.Success(
            new DeactivateCategoryResult(category.Id, category.IsActive));
    }
}
