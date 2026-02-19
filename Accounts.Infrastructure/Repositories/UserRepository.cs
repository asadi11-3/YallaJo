using Accounts.Domain.Entities;
using Accounts.Domain.Interfaces;
using Accounts.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

    public sealed class UserRepository(AccountsDbContext context)
        : EfRepository<User, Guid>(context), IUserRepository
    {
    }
