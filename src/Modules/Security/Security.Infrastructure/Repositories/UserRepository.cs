using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Infrastructure.Repositories;

internal sealed class UserRepository(SecurityDbContext context)
    : EfRepository<User, Guid>(context), IUserRepository
{
    public async Task<User?> GetByIdWithEmailsAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Emails)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<User?> GetByEmailWithDetailsAsync(string normalizedEmail, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Emails)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleClaims)
            .Include(u => u.UserClaims)
            .FirstOrDefaultAsync(
                u => u.Emails.Any(e => e.Address == normalizedEmail && e.IsPrimary),
                ct);
    }

    public async Task<User?> GetByIdWithDetailsAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Emails)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleClaims)
            .Include(u => u.UserClaims)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<User?> GetByIdWithPhonesAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Phones)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<bool> AnyWithRoleAsync(string roleName, CancellationToken ct = default)
     => await context.UserRoles
            .Where(ur => ur.Role.Name == roleName)
            .AnyAsync(ct);

    public async Task<int> CountUsersInRolesExcludingAssignmentAsync(
        IReadOnlyCollection<string> roleNames,
        Guid excludedUserId,
        Guid excludedRoleId,
        CancellationToken ct = default)
    {
        if (roleNames.Count == 0)
            return 0;

        return await context.UserRoles
            .AsNoTracking()
            .Where(ur => roleNames.Contains(ur.Role.Name))
            .Where(ur => ur.UserId != excludedUserId || ur.RoleId != excludedRoleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        => await context.UserRoles
               .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

    public void RemoveUserRole(UserRole userRole)
        => context.UserRoles.Remove(userRole);

    public Task<PaginatedResult<User>> GetPagedWithDetailsAsync(
        int page, int pageSize, CancellationToken ct = default)
        => GetPaginatedAsync(
            pageNumber: page,
            pageSize: pageSize,
            include: q => q
                .Include(u => u.Emails.Where(e => e.IsPrimary))
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role),
            orderBy: q => q.OrderBy(u => u.Id),
            asNoTracking: true,
            ct: ct);

    public async Task<Guid?> GetUserIdByEmailAsync(string normalizedEmail, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Emails.Any(e => e.Address == normalizedEmail && e.IsPrimary))
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> GetPrimaryPhoneNumberAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Phones
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsPrimary)
            .Select(p => (string?)p.PhoneNumber)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (string?)u.PasswordHash)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(
        string? query,
        IReadOnlyCollection<Guid>? ids,
        int limit,
        CancellationToken ct = default)
    {
        var users = context.Users
            .AsNoTracking()
            .Include(u => u.Emails.Where(e => e.IsPrimary))
            .Where(u => u.IsActive);

        if (ids is { Count: > 0 })
        {
            users = users.Where(u => ids.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Email addresses are stored lowercased; normalize the term the same way.
            var term = query.Trim().ToLowerInvariant();
            users = users.Where(u => u.Emails.Any(e => e.IsPrimary && e.Address.Contains(term)));
        }

        return await users
            .OrderBy(u => u.Id)
            .Take(limit)
            .ToListAsync(ct);
    }
}
