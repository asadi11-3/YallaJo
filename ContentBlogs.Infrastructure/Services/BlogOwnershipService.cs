using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Infrastructure.Services;

/// <summary>
/// Cross-module ownership probe for the Blog aggregate. Uses a no-tracking
/// projection that ignores soft-delete query filters so callers can distinguish
/// missing rows from soft-deleted rows.
/// </summary>
internal sealed class BlogOwnershipService(ContentBlogsDbContext dbContext) : IBlogOwnershipService
{
    public async Task<EntityOwnershipResolution> GetBlogOwnershipAsync(
        Guid blogId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Blog>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Id == blogId)
            .Select(b => new { b.AuthorId, b.IsDeleted })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return new EntityOwnershipResolution(
                IsSupported: true,
                Exists: false,
                IsDeleted: false,
                OwnerUserId: null);
        }

        return new EntityOwnershipResolution(
            IsSupported: true,
            Exists: true,
            IsDeleted: row.IsDeleted,
            OwnerUserId: row.AuthorId);
    }
}
