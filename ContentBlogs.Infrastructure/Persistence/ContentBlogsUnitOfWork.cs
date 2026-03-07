using ContentBlogs.Application.Interfaces;

namespace ContentBlogs.Infrastructure.Persistence;

internal sealed class ContentBlogsUnitOfWork(ContentBlogsDbContext context) : IContentBlogsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
