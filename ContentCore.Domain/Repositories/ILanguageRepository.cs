using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="Language"/> aggregates.
///
/// Inherits the complete SharedKernel read/write surface via
/// <see cref="IRepository{TEntity,TKey}"/> (which requires <c>IAggregateRoot</c>).
///
/// No additional methods are declared here because all current operations are
/// covered by the inherited generic API (GetByIdAsync, GetAllAsync, AnyAsync, etc.).
/// Language-code uniqueness is verified inline at call sites via
/// <c>AnyAsync(l =&gt; l.Code == code)</c>.
/// </summary>
public interface ILanguageRepository : IRepository<Language, Guid>
{
}
