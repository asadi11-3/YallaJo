using ContentBlogs.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentBlogs.Infrastructure.Persistence;

internal sealed class ContentBlogsUnitOfWork(IUnitOfWork<ContentBlogsDbContext> inner) : IContentBlogsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => inner.SaveChangesAsync(ct);
}
