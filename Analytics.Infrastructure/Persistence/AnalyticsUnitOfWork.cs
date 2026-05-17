using Analytics.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Analytics.Infrastructure.Persistence;

internal sealed class AnalyticsUnitOfWork(IUnitOfWork<AnalyticsDbContext> inner) : IAnalyticsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}
