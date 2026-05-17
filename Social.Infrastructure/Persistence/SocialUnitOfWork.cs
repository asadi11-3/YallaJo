using Social.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Social.Infrastructure.Persistence;

internal sealed class SocialUnitOfWork(IUnitOfWork<SocialDbContext> inner) : ISocialUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}
