using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
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

    private static string NormalizeSlug(string slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();
}
