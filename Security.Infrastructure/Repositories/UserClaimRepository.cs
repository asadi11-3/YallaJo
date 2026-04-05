using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Security.Infrastructure.Repositories;

internal sealed class UserClaimRepository(SecurityDbContext context)
    : EfEntityRepository<UserClaim, Guid>(context), IUserClaimRepository
{
}
