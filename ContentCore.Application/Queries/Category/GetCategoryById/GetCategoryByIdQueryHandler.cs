using ContentCore.Application.Queries.Category.ListCategories;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<Result<CategoryDto>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Load category — include Translations when client requested them
            var category = request.WithTranslations
                ? await categoryRepository.GetAsync(
                    filter: c => c.Id == request.Id,
                    include: q => q.Include(c => c.Translations),
                    ct: cancellationToken)
                : await categoryRepository.GetByIdAsync(request.Id, cancellationToken);

            if (category is null || !category.IsActive)
            {
                return Result<CategoryDto>.Failure(
                    new Error("Category.NotFound", $"Category '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            var subcategories = request.WithTranslations
                ? await categoryRepository.GetAllAsync(
                    filter: c => c.ParentCategoryId == request.Id,
                    include: q => q.Include(c => c.Translations),
                    orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
                    ct: cancellationToken)
                : await categoryRepository.GetAllAsync(
                    filter: c => c.ParentCategoryId == request.Id,
                    orderBy: q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name),
                    ct: cancellationToken);

            var childDtos = subcategories
                .Select(c => CategoryDto.From(c, []))
                .ToList() as IReadOnlyList<CategoryDto>;

            return Result<CategoryDto>.Success(CategoryDto.From(category, childDtos));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CategoryDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
