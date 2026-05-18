using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

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

    Task<Blog?> GetFeaturedBlogInPlaceScopeAsync(
        Guid? placeId,
        Guid? excludeBlogId,
        CancellationToken cancellationToken = default);

    Task<Blog?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<Blog>> GetDeletedBlogsAsync(
        int page,
        int pageSize,
        BlogStatus? status,
        Guid? placeId,
        string? search,
        string sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Blog>> GetActiveByPlaceIdAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);
}
