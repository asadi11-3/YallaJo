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

            // Enforce max 3 levels: Root -> Sub -> Sub-Sub
            if (parent.ParentCategoryId.HasValue)
            {
                var grandparent = await categoryRepository.GetByIdAsync(parent.ParentCategoryId.Value, ct);
                if (grandparent is not null && grandparent.ParentCategoryId.HasValue)
                {
                    return Result<CreateCategoryResult>.Failure(
                        new Error(
                            "Category.MaxDepthExceeded",
                            "Cannot create category: maximum depth of 3 levels exceeded."));
                }
            }
        }

        // 2. Auto-generate slug from name if not provided
        var slug = request.Slug ?? GenerateSlug(request.Name);

        // 3. Create the category (domain event raised inside Create())
        var category = Domain.Entities.Category.Create(
            request.Name,
            slug,
            request.SourceLanguageCode,
            request.ParentCategoryId,
            request.SortOrder);

        // 4. Set optional properties
        if (request.Icon is not null)
            category.SetIcon(request.Icon);

        // 5. Persist (UoW dispatches domain events during SaveChanges)
        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateCategoryResult>.Created(
            new CreateCategoryResult(category.Id, category.Name, category.Slug));
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant().Replace(' ', '-'),
            @"[^a-z0-9\-]", string.Empty)
        .Trim('-');
}
