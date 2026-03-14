using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<Result<UpdateCategoryResult>> Handle(
        UpdateCategoryCommand request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);

        if (category is null)
            return Result<UpdateCategoryResult>.NotFound($"Category '{request.Id}' not found.");

        // Validate depth if parent is being changed
        if (request.ParentCategoryId.HasValue &&
            request.ParentCategoryId != category.ParentCategoryId)
        {
            var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
            if (parent is null)
                return Result<UpdateCategoryResult>.NotFound(
                    $"Parent category '{request.ParentCategoryId}' not found.");

            if (parent.ParentCategoryId.HasValue)
            {
                var grandparent = await categoryRepository.GetByIdAsync(parent.ParentCategoryId.Value, ct);
                if (grandparent?.ParentCategoryId.HasValue == true)
                    return Result<UpdateCategoryResult>.Failure(
                        new Error("Category.MaxDepthExceeded",
                            "Cannot move category: maximum depth of 3 levels exceeded."));
            }

            category.ChangeParent(request.ParentCategoryId);
        }

        // Update name, slug, source language (raises CategoryUpdatedDomainEvent for auto-translation)
        category.Update(request.Name, request.Slug, request.SourceLanguageCode);

        if (request.Icon is not null)
            category.SetIcon(request.Icon);

        if (request.SortOrder.HasValue)
            category.SetSortOrder(request.SortOrder.Value);

        // Apply manual translation overrides if provided
        if (request.Translations is not null)
        {
            foreach (var t in request.Translations)
            {
                var existing = category.Translations
                    .FirstOrDefault(x => x.LanguageId == t.LanguageId);

                if (existing is not null)
                    category.UpdateTranslation(t.LanguageId, t.Name, t.Slug);
                else
                    category.AddTranslation(t.LanguageId, t.Name, t.Slug);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateCategoryResult>.Success(
            new UpdateCategoryResult(category.Id, category.Name, category.Slug));
    }
}
