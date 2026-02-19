using Accounts.Domain.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Data;

namespace Accounts.Infrastructure.Persistence;

internal sealed class AccountsUnitOfWork(IUnitOfWork<AccountsDbContext> unitOfWork)
    : IAccountsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => unitOfWork.SaveChangesAsync(ct);
}
