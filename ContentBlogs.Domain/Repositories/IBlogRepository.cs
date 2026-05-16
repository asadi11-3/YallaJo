using ContentBlogs.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface IBlogRepository : IRepository<Blog, Guid>
{
    Task<bool> IsSlugReservedAsync(
        string slug,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<Blog?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<bool> IncrementViewCountIfPublishedAsync(
        Guid blogId,
        CancellationToken cancellationToken = default);

    Task<Blog?> GetFeaturedBlogInPlaceScopeAsync(
        Guid? placeId,
        Guid? excludeBlogId,
        CancellationToken cancellationToken = default);
}
