using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure.Persistence;

internal sealed class AuthUnitOfWork(IUnitOfWork<AuthDbContext> unitOfWork) : IAuthUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => unitOfWork.SaveChangesAsync(ct);
}
