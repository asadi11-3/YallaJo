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
        var ids = request.Items.Select(x => x.CategoryId).ToList();

        // Single DB query: fetch all requested categories at once — O(1) roundtrip vs O(n)
        var categories = await categoryRepository.GetAllAsync(
            filter: c => ids.Contains(c.Id),
            asNoTracking: false,
            ct: ct);

        // Fail fast if any ID is missing
        if (categories.Count != ids.Count)
        {
            var missing = ids.Except(categories.Select(c => c.Id)).First();
            return Result.NotFound($"Category '{missing}' not found.");
        }

        // Build O(1) lookup for sort order assignment
        var sortOrderMap = request.Items.ToDictionary(x => x.CategoryId, x => x.SortOrder);

        foreach (var category in categories)
            category.SetSortOrder(sortOrderMap[category.Id]);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
