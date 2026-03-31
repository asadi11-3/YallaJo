using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="Category"/> aggregates.
///
/// Inherits the complete SharedKernel read/write surface via
/// <see cref="IRepository{TEntity,TKey}"/> (which requires <c>IAggregateRoot</c>).
///
/// No additional methods are declared here because all current query and write
/// operations are covered by the inherited generic API
/// (GetByIdAsync, GetAllAsync, GetAsync, AnyAsync, AddAsync, etc.).
/// Domain-specific include patterns (e.g. loading Translations) are expressed
/// inline at the call site via the <c>include</c> parameter.
/// </summary>
public interface ICategoryRepository : IRepository<Category, Guid>
{
}
