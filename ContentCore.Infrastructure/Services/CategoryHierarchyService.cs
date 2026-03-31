using ContentCore.Domain.Repositories;
using ContentCore.Domain.Services;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ICategoryHierarchyService"/>.
/// Uses <see cref="ICategoryRepository"/> to traverse the category tree.
/// Maximum traversal depth is capped at 10 to guard against circular data.
/// </summary>
internal sealed class CategoryHierarchyService(ICategoryRepository categoryRepository)
    : ICategoryHierarchyService
{
    /// <inheritdoc/>
    public int MaxDepth => 3;

    private const int MaxTraversalDepth = 10;

    /// <inheritdoc/>
    public async Task<int> GetDepthAsync(Guid? categoryId, CancellationToken ct = default)
    {
        if (categoryId is null)
            return 0;

        var depth = 0;
        var currentId = categoryId;

        for (var i = 0; i < MaxTraversalDepth; i++)
        {
            if (currentId is null) break;
            var cat = await categoryRepository.GetByIdAsync(currentId.Value, ct);
            if (cat is null) break;
            depth++;
            currentId = cat.ParentCategoryId;
        }

        return depth;
    }

    /// <inheritdoc/>
    public async Task<int> GetSubtreeHeightAsync(Guid categoryId, CancellationToken ct = default)
    {
        var children = await categoryRepository.GetAllAsync(
            filter: c => c.ParentCategoryId == categoryId,
            ct: ct);

        if (children.Count == 0)
            return 0;

        var maxChildHeight = 0;
        foreach (var child in children)
        {
            var childHeight = await GetSubtreeHeightAsync(child.Id, ct);
            if (childHeight > maxChildHeight)
                maxChildHeight = childHeight;
        }

        return maxChildHeight + 1;
    }

    /// <inheritdoc/>
    /// ISSUE-002 / B1: Moved from the Application-layer loop in UpdateCategoryCommandHandler.
    public async Task<bool> IsAncestorAsync(
        Guid ancestorCandidateId,
        Guid descendantId,
        CancellationToken ct = default)
    {
        var currentId = (Guid?)ancestorCandidateId;

        for (var i = 0; i < MaxTraversalDepth; i++)
        {
            if (currentId is null)
                return false;

            var cat = await categoryRepository.GetByIdAsync(currentId.Value, ct);
            if (cat is null || !cat.ParentCategoryId.HasValue)
                return false;

            if (cat.ParentCategoryId.Value == descendantId)
                return true;

            currentId = cat.ParentCategoryId;
        }

        return false;
    }
}
