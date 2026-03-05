using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

internal sealed class UserClaimRepository(SecurityDbContext context)
    : EfEntityRepository<UserClaim, Guid>(context), IUserClaimRepository
{
    public Task<bool> ExistsAsync(
        Guid userId, string claimType, string claimValue, CancellationToken ct = default)
        => AnyAsync(
            uc => uc.UserId == userId
               && uc.ClaimType == claimType
               && uc.ClaimValue == claimValue,
            ct);
}
