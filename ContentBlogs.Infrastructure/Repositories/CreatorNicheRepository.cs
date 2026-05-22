using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorNicheRepository(ContentBlogsDbContext context) : ICreatorNicheRepository
{
    public Task<CreatorNiche?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorNiches
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<List<CreatorNiche>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorNiches
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return context.CreatorNiches
            .AnyAsync(
                n => n.Slug == normalizedSlug &&
                     (!excludeId.HasValue || n.Id != excludeId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        CreatorNiche niche,
        CancellationToken cancellationToken = default)
    {
        await context.CreatorNiches.AddAsync(niche, cancellationToken).ConfigureAwait(false);
    }

    public void Update(CreatorNiche niche)
    {
        context.CreatorNiches.Update(niche);
    }

    public async Task<bool> AllExistAndActiveAsync(
        IReadOnlyList<Guid> nicheIds,
        CancellationToken cancellationToken = default)
    {
        if (nicheIds.Count == 0)
            return true;

        var activeCount = await context.CreatorNiches
            .CountAsync(n => nicheIds.Contains(n.Id) && n.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return activeCount == nicheIds.Count;
    }
}
