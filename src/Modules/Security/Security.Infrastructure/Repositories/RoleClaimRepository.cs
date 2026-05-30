using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for <see cref="RoleClaim"/> entities.
///
/// RoleClaim is a child entity (not an aggregate root) — it uses
/// <see cref="EfEntityRepository{TEntity,TKey}"/> which has no IAggregateRoot constraint.
///

internal sealed class RoleClaimRepository(SecurityDbContext context)
    : EfEntityRepository<RoleClaim, Guid>(context), IRoleClaimRepository
{
}
