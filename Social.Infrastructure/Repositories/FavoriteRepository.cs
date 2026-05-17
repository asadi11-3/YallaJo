using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Social.Infrastructure.Repositories;

internal sealed class FavoriteRepository(SocialDbContext context) : EfRepository<Favorite, Guid>(context), IFavoriteRepository
{
    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.Favorites.AsNoTracking().Where(f => f.UserId == userId).ToListAsync(ct).ConfigureAwait(false);

    public Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default)
        => context.Favorites.CountAsync(f => f.UserId == userId, ct);

    public Task<Favorite?> GetByUserAndEntityAsync(Guid userId, string entityType, Guid entityId, CancellationToken ct = default)
        => context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.EntityType == entityType && f.EntityId == entityId, ct);
}
