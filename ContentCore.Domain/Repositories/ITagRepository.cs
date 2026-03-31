using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository contract for the <see cref="Tag"/> entity.
///
/// <see cref="Tag"/> is NOT an <c>IAggregateRoot</c>, so this interface inherits
/// <see cref="IReadRepository{TEntity,TKey}"/> and <see cref="IWriteRepository{TEntity,TKey}"/>
/// separately rather than the combined <c>IRepository</c>.
///
/// No additional methods are declared here because all current operations are
/// covered by the inherited generic API (GetByIdAsync, GetAllAsync, AnyAsync, etc.).
/// Slug uniqueness is verified inline at call sites via
/// <c>AnyAsync(t =&gt; t.Slug == slug)</c>.
/// </summary>
public interface ITagRepository : IReadRepository<Tag, Guid>, IWriteRepository<Tag, Guid>
{
}
