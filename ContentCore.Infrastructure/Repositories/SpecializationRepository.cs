using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for the <see cref="Specialization"/> entity.
/// <see cref="Specialization"/> is not an <c>IAggregateRoot</c>, so this class extends
/// <see cref="EfEntityRepository{TEntity,TKey}"/> rather than <see cref="EfRepository{TEntity,TKey}"/>.
/// </summary>
internal sealed class SpecializationRepository(ContentCoreDbContext context)
    : EfEntityRepository<Specialization, Guid>(context), ISpecializationRepository;
