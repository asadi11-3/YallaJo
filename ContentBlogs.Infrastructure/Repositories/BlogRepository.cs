using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class BlogRepository(ContentBlogsDbContext context)
    : EfRepository<Blog, Guid>(context), IBlogRepository
{
    public Task<bool> IsSlugReservedAsync(
        string slug,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);

        return AnyAsync(
            blog => blog.Slug == normalizedSlug &&
                (!excludeId.HasValue || blog.Id != excludeId.Value),
            cancellationToken);
    }

    public Task<Blog?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);

        return FirstOrDefaultAsync(
            blog => blog.Slug == normalizedSlug,
            ct: cancellationToken);
    }

    public async Task<bool> IncrementViewCountIfPublishedAsync(
        Guid blogId,
        CancellationToken cancellationToken = default)
    {
        var rowsAffected = await context.Blogs
            .Where(b => b.Id == blogId && b.Status == BlogStatus.Published)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(b => b.ViewCount, b => b.ViewCount + 1),
                cancellationToken)
            .ConfigureAwait(false);

        return rowsAffected > 0;
    }

    public Task<Blog?> GetFeaturedBlogInPlaceScopeAsync(
        Guid? placeId,
        Guid? excludeBlogId,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(
            blog => blog.IsFeatured
                 && blog.PlaceId == placeId
                 && (excludeBlogId == null || blog.Id != excludeBlogId.Value),
            ct: cancellationToken);
    }

    private static string NormalizeSlug(string slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();
}
