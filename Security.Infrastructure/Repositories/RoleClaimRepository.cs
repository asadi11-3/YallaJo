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
/// All read methods (GetAllAsync, AnyAsync, …) are inherited directly from
/// EfEntityRepository → EfReadRepository. No _read field needed.
/// </summary>
internal sealed class RoleClaimRepository(SecurityDbContext context)
    : EfEntityRepository<RoleClaim, Guid>(context), IRoleClaimRepository
{
    public async Task<IReadOnlyList<RoleClaim>> GetClaimsByRoleIdAsync(
        Guid roleId, CancellationToken ct = default)
        => (await GetAllAsync(
            filter: rc => rc.RoleId == roleId,
            asNoTracking: true,
            ct: ct)).AsReadOnly();

    public Task<bool> ExistsAsync(
        Guid roleId, string claimType, string claimValue, CancellationToken ct = default)
        => AnyAsync(
            rc => rc.RoleId == roleId
               && rc.ClaimType == claimType
               && rc.ClaimValue == claimValue,
            ct);

    public async Task<IReadOnlyList<RoleClaim>> GetClaimsByRoleIdsAsync(
        IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        var ids = roleIds?.ToList() ?? [];
        if (ids.Count == 0) return [];

        return (await GetAllAsync(
            filter: rc => ids.Contains(rc.RoleId),
            asNoTracking: true,
            ct: ct)).AsReadOnly();
    }

    public async Task<IReadOnlyList<string>> GetClaimValuesByTypeAsync(
        Guid roleId, string claimType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(claimType)) return [];

        var claims = await GetAllAsync(
            filter: rc => rc.RoleId == roleId
                       && rc.ClaimType == claimType
                       && rc.ClaimValue != null,
            asNoTracking: true,
            ct: ct);

        return claims.Select(rc => rc.ClaimValue!).ToList().AsReadOnly();
    }
}
