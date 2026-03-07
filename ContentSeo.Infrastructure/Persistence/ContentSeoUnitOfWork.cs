using ContentSeo.Application.Interfaces;

namespace ContentSeo.Infrastructure.Persistence;

internal sealed class ContentSeoUnitOfWork(ContentSeoDbContext context) : IContentSeoUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
