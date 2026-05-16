using ContentBlogs.Domain.Entities;

namespace ContentBlogs.Domain.Repositories;

public interface IBlogTourRepository
{
    Task<IReadOnlySet<Guid>> GetLinkedTourIdsAsync(
        Guid blogId,
        CancellationToken cancellationToken = default);

    Task<int> CountByBlogIdAsync(
        Guid blogId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<BlogTour> links,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(
        Guid blogId,
        Guid tourId,
        CancellationToken cancellationToken = default);
}
