using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed class ReactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<ReactivateCategoryCommand, ReactivateCategoryResult>
{
    public async Task<Result<ReactivateCategoryResult>> Handle(
        ReactivateCategoryCommand request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (category is null)
            return Result<ReactivateCategoryResult>.NotFound(
                $"Category '{request.Id}' not found.");

        category.Activate();
        await unitOfWork.SaveChangesAsync(ct);

        DeleteCategoryCommandHandler.InvalidateCategoryCache(cache, request.Id);

        return Result<ReactivateCategoryResult>.Success(
            new ReactivateCategoryResult(category.Id, category.IsActive));
    }
}
