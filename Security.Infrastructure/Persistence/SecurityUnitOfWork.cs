using Security.Domain.Repositories;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure.Persistence;

internal sealed class SecurityUnitOfWork(IUnitOfWork<SecurityDbContext> unitOfWork)
    : ISecurityUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => unitOfWork.SaveChangesAsync(ct);
}
