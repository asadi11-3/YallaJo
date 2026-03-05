using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for <see cref="Role"/> entities.
///
/// Role is a child entity (not an aggregate root) — it uses
/// <see cref="EfEntityRepository{TEntity,TKey}"/> which has no IAggregateRoot constraint.
/// No ASP.NET Identity / RoleManager dependency.
/// </summary>
internal sealed class RoleRepository(SecurityDbContext context)
    : EfEntityRepository<Role, Guid>(context), IRoleRepository
{
    // Typed context for joins that need SecurityDbContext-specific DbSets.
    private readonly SecurityDbContext _db = context;

    /// <summary>
    /// Returns a map of userId → role-name list for the given user IDs.
    /// One batch query — no N+1.
    /// </summary>
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

    /// <summary>
    /// Loads active roles matching any of the supplied names (case-insensitive).
    /// </summary>
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

        // names.Contains translates to SQL IN — EF Core handles it.
        return (await GetAllAsync(
            filter: r => names.Contains(r.Name),
            asNoTracking: true,
            ct: ct)).AsReadOnly();
    }
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
       => await FirstOrDefaultAsync(r => r.Name == name, ct: ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await AnyAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken ct = default)
        => (await GetAllAsync(filter: r => r.IsActive, asNoTracking: true, ct: ct)).AsReadOnly();
}
