using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed class ReorderCategoriesCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ReorderCategoriesCommand, ReorderCategoriesResult>
{
    public async Task<Result<ReorderCategoriesResult>> Handle(
        ReorderCategoriesCommand request,
        CancellationToken ct)
    {
        //  إذا ما في عناصر، نرجّع 0 بدون خطأ
        if (request.Items is null || request.Items.Count == 0)
        {
            return Result<ReorderCategoriesResult>.Success(
                new ReorderCategoriesResult(0));
        }

        var ids = request.Items
            .Select(x => x.Id)
            .ToList();

        //  نجيب الكاتيجوري tracked حتى نعدل SortOrder
        var categories = await categoryRepository.GetByIdsAsync(
            ids,
            ct,
            asNoTracking: false);

        //  لو في العناصر غير موجودة نرجّع NotFound
        if (categories.Count != ids.Count)
        {
            return Result<ReorderCategoriesResult>.NotFound(
                "One or more categories were not found.");
        }

        var sortOrderMap = request.Items.ToDictionary(
            x => x.Id,
            x => x.SortOrder);

        foreach (var category in categories)
        {
            if (sortOrderMap.TryGetValue(category.Id, out var sortOrder))
            {
                // نحدّث ترتيب كل عنصر
                category.SetSortOrder(sortOrder);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result<ReorderCategoriesResult>.Success(
            new ReorderCategoriesResult(categories.Count));
    }
}