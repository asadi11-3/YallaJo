namespace ContentBlogs.Application.Interfaces;

public interface IContentBlogsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
