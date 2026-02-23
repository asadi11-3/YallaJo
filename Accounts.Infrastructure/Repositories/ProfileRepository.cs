using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using Accounts.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class ProfileRepository(AccountsDbContext context)
    : EfRepository<Profile, Guid>(context), IProfileRepository
{
}
