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
}