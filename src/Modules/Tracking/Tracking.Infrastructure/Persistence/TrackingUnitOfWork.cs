using Tracking.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Tracking.Infrastructure.Persistence;

internal sealed class TrackingUnitOfWork(IUnitOfWork<TrackingDbContext> inner) : ITrackingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}
