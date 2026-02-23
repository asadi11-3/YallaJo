using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

public sealed class UserRepository(SecurityDbContext context)
    : EfRepository<User, Guid>(context), IUserRepository
{
    public async Task<User?> GetByIdWithEmailsAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Emails)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }
}
