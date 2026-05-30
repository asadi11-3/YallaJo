using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

internal sealed class RoleRepository(SecurityDbContext context)
    : EfEntityRepository<Role, Guid>(context), IRoleRepository
{
    private readonly SecurityDbContext _db = context;

    public async Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesByUserIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds?.ToList() ?? [];
        if (ids.Count == 0) return [];

        var pairs = await _db.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(_db.Roles,
                ur => ur.RoleId,
                r  => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync(ct);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.RoleName).ToList());
    }

    public async Task<Role?> GetByIdWithClaimsAsync(Guid roleId, CancellationToken ct = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .Include(r => r.RoleClaims)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
        IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        if (roleNames is null) return [];

        var names = roleNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0) return [];

        return (await GetAllAsync(
            filter: r => names.Contains(r.Name),
            asNoTracking: true,
            ct: ct)).AsReadOnly();
    }
}
