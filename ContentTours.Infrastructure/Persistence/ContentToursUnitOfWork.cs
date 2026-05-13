using ContentTours.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence;

internal sealed class ContentToursUnitOfWork(
    IUnitOfWork<ContentToursDbContext> inner) : IContentToursUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
