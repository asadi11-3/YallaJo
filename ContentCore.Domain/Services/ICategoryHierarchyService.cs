namespace ContentCore.Domain.Services;

/// <summary>
/// Domain service for computing category tree depths and subtree heights.
/// Used by command handlers to enforce the maximum hierarchy depth invariant.
/// </summary>
public interface ICategoryHierarchyService
{
    /// <summary>
    /// The maximum allowed depth of the category tree (e.g. 3 = root → child → grandchild).
    /// All hierarchy enforcement must read this value rather than hard-coding the limit.
    /// </summary>
    int MaxDepth { get; }

    /// <summary>
    /// Returns the 1-based depth of the category identified by <paramref name="categoryId"/>.
    /// A root category (no parent) has depth 1. Returns 0 when <paramref name="categoryId"/> is null.
    /// </summary>
    Task<int> GetDepthAsync(Guid? categoryId, CancellationToken ct = default);

    /// <summary>
    /// Returns the height of the subtree rooted at <paramref name="categoryId"/>.
    /// A leaf node (no children) returns 0. A node with only leaf children returns 1.
    /// </summary>
    Task<int> GetSubtreeHeightAsync(Guid categoryId, CancellationToken ct = default);
}
