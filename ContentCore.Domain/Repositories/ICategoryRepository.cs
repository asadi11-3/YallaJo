using ContentCore.Domain.Entities;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    /// <summary>
    /// Loads categories with their Translations collection eagerly populated.
    /// Use in query handlers that return language-aware responses.
    /// </summary>
    Task<List<Category>> GetAllWithTranslationsAsync(
        Expression<Func<Category, bool>>? filter = null,
        Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Loads a single category by ID with its Translations collection eagerly populated.
    /// </summary>
    Task<Category?> GetByIdWithTranslationsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns true if any non-deleted category already uses the given slug.
    /// Used to pre-validate uniqueness before persisting, producing a clean Conflict
    /// result instead of a DB exception.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Returns true if any non-deleted category other than <paramref name="excludeId"/>
    /// already uses the given slug.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid excludeId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the category has at least one direct child.
    /// Used to guard against deleting a parent that still has subcategories.
    /// </summary>
    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken ct = default);
}