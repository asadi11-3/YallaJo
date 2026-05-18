using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
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

    public Task<Blog?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return context.Blogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<PaginatedResult<Blog>> GetDeletedBlogsAsync(
        int page,
        int pageSize,
        BlogStatus? status,
        Guid? placeId,
        string? search,
        string sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToLowerInvariant();

        var query = context.Blogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.IsDeleted);

        if (status.HasValue)
        {
            var statusValue = status.Value;
            query = query.Where(b => b.Status == statusValue);
        }

        if (placeId.HasValue)
        {
            var placeIdValue = placeId.Value;
            query = query.Where(b => b.PlaceId == placeIdValue);
        }

        if (normalizedSearch is not null)
        {
            query = query.Where(b =>
                b.Slug.Contains(normalizedSearch) || b.Title.Contains(normalizedSearch));
        }

        query = ApplyDeletedSort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = totalCount == 0
            ? []
            : await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return new PaginatedResult<Blog>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<Blog>> GetActiveByPlaceIdAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var list = await context.Blogs
            .Where(b => b.PlaceId == placeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task<IReadOnlyList<Blog>> GetActiveByTourIdAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var list = await context.Blogs
            .Where(b => b.BlogTours.Any(bt => bt.TourId == tourId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public Task<bool> HasTranslationForLanguageAsync(
        Guid blogId,
        Guid languageId,
        CancellationToken cancellationToken = default)
    {
        return context.BlogTranslations
            .AnyAsync(t => t.BlogId == blogId && t.LanguageId == languageId, cancellationToken);
    }

    private static IQueryable<Blog> ApplyDeletedSort(
        IQueryable<Blog> query,
        string sortBy,
        bool sortDescending)
    {
        // Accepted sort keys; anything else falls through to DeletedAt desc.
        return sortBy switch
        {
            "title" => sortDescending
                ? query.OrderByDescending(b => b.Title)
                : query.OrderBy(b => b.Title),
            "updatedAt" => sortDescending
                ? query.OrderByDescending(b => b.UpdatedAt)
                : query.OrderBy(b => b.UpdatedAt),
            // Default + explicit "deletedAt"
            _ => sortDescending
                ? query.OrderByDescending(b => b.DeletedAt)
                : query.OrderBy(b => b.DeletedAt),
        };
    }

    private static string NormalizeSlug(string slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();
}
