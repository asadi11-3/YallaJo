using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed class ReorderCategoriesCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ReorderCategoriesCommand>
{
    public async Task<Result> Handle(
        ReorderCategoriesCommand request,
        CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            var category = await categoryRepository.GetByIdAsync(item.CategoryId, ct);
            if (category is null)
                return Result.NotFound($"Category '{item.CategoryId}' not found.");

            category.SetSortOrder(item.SortOrder);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
