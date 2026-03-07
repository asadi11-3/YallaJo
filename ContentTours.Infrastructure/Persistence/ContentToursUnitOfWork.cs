using ContentTours.Application.Interfaces;

namespace ContentTours.Infrastructure.Persistence;

internal sealed class ContentToursUnitOfWork(ContentToursDbContext context) : IContentToursUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
