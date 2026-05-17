using Finance.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Finance.Infrastructure.Persistence;

internal sealed class FinanceUnitOfWork(IUnitOfWork<FinanceDbContext> inner) : IFinanceUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}
