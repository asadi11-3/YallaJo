using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

/// <summary>
/// Repository contract for the <see cref="Specialization"/> entity.
///
/// <see cref="Specialization"/> is NOT an <c>IAggregateRoot</c>, so this interface
/// inherits <see cref="IReadRepository{TEntity,TKey}"/> and
/// <see cref="IWriteRepository{TEntity,TKey}"/> separately rather than the combined
/// <c>IRepository</c>.
///
/// No additional methods are declared here because all current operations are
/// covered by the inherited generic API (GetByIdAsync, GetAllAsync, AddAsync, etc.).
/// </summary>
public interface ISpecializationRepository : IReadRepository<Specialization, Guid>, IWriteRepository<Specialization, Guid>
{
}
