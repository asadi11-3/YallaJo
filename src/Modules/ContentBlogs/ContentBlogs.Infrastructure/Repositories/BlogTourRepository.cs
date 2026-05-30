using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Infrastructure.Repositories;

public sealed class BlogTourRepository(ContentBlogsDbContext dbContext)
    : IBlogTourRepository
{
    public async Task<IReadOnlySet<Guid>> GetLinkedTourIdsAsync(
        Guid blogId,
        CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.BlogTours
            .AsNoTracking()
            .Where(bt => bt.BlogId == blogId)
            .Select(bt => bt.TourId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids.ToHashSet();
    }

    public Task<int> CountByBlogIdAsync(
        Guid blogId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.BlogTours
            .CountAsync(bt => bt.BlogId == blogId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<BlogTour> links,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(links);
        dbContext.BlogTours.AddRange(links);
        return Task.CompletedTask;
    }

    public async Task<bool> RemoveAsync(
        Guid blogId,
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.BlogTours
            .FindAsync([blogId, tourId], cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            return false;

        dbContext.BlogTours.Remove(existing);
        return true;
    }
}
