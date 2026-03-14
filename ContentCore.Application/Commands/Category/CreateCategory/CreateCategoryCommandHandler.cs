using ContentCore.Application.Caching;
using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<Result<CreateCategoryResult>> Handle(
        CreateCategoryCommand request,
        CancellationToken ct)
    {
        // Auto-generate slug from name when not provided
        var slug = request.Slug ?? GenerateSlug(request.Name);

        // Validate parent + enforce max 3-level depth
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
            if (parent is null)
                return Result<CreateCategoryResult>.NotFound(
                    $"Parent category '{request.ParentCategoryId}' not found.");

            if (parent.ParentCategoryId.HasValue)
            {
                var grandparent = await categoryRepository.GetByIdAsync(parent.ParentCategoryId.Value, ct);
                if (grandparent?.ParentCategoryId.HasValue == true)
                    return Result<CreateCategoryResult>.Failure(
                        new Error("Category.MaxDepthExceeded",
                            "Cannot create category: maximum depth of 3 levels exceeded."));
            }
        }

        var category = Domain.Entities.Category.Create(
            request.Name, slug, request.SourceLanguageCode,
            request.ParentCategoryId, request.SortOrder);

        await categoryRepository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate all root-level category list caches
        foreach (var key in ContentCoreCacheKeys.CommonCategoryListKeys())
            cache.Remove(key);

        return Result<CreateCategoryResult>.Created(
            new CreateCategoryResult(category.Id, category.Name, category.Slug));
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", string.Empty)
            .Trim('-');
}
