using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<Result<CreateCategoryResult>> Handle(
        CreateCategoryCommand request,
        CancellationToken ct)
    {
        // 1. Validate parent exists (if specified)
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
            if (parent is null)
                return Result<CreateCategoryResult>.NotFound(
                    $"Parent category '{request.ParentCategoryId}' not found.");
        }

        // 2. Create the category (domain event raised inside Create())
        var category = Domain.Entities.Category.Create(
            request.Name,
            request.Slug,
            request.SourceLanguageCode,
            request.ParentCategoryId,
            request.SortOrder);

        // 3. Set optional properties
        if (request.Icon is not null)
            category.SetIcon(request.Icon);

        // 4. Persist (UoW dispatches domain events during SaveChanges)
        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateCategoryResult>.Created(
            new CreateCategoryResult(category.Id, category.Name, category.Slug));
    }
}
