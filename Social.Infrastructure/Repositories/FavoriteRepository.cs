using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class FavoriteRepository(SocialDbContext context)
    : EfRepository<Favorite, Guid>(context), IFavoriteRepository
{
    public async Task<(IReadOnlyList<Favorite> Items, Guid? NextCursor)> GetByUserPageAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var query = context.Favorites.AsNoTracking().Where(f => f.UserId == userId);

        if (afterId.HasValue)
            query = query.Where(f => f.Id.CompareTo(afterId.Value) < 0);

        var items = await query
            .OrderByDescending(f => f.Id)
            .Take(size + 1)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Guid? nextCursor = null;
        if (items.Count > size)
        {
            nextCursor = items[size].Id;
            items = items.Take(size).ToList();
        }

        return (items, nextCursor);
    }

    public Task<Favorite?> GetByUserAndEntityAsync(
        Guid userId, FavoriteEntityType entityType, Guid entityId, CancellationToken ct = default)
        => context.Favorites
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId
                                      && f.EntityType == entityType
                                      && f.EntityId == entityId, ct);

    public Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default)
        => context.Favorites.CountAsync(f => f.UserId == userId, ct);

    public async Task<IReadOnlyList<Favorite>> GetByEntityAsync(
        FavoriteEntityType entityType, Guid entityId, CancellationToken ct = default)
        => await context.Favorites
            .AsNoTracking()
            .Where(f => f.EntityType == entityType && f.EntityId == entityId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
