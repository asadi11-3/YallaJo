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
}
