using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class ProfileRepository(AccountsDbContext context)
    : EfRepository<Profile, Guid>(context), IProfileRepository
{
    public async Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => await context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct)
        => await context.Profiles.AnyAsync(p => p.UserId == userId, ct);
}
