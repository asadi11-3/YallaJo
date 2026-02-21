using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Auth.Infrastructure.Repositories;

public sealed class UserCredentialsRepository(AuthDbContext context)
    : EfRepository<UserCredentials, Guid>(context), IUserCredentialsRepository
{
    public async Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await context.UserCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);
}
