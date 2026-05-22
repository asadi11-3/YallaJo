using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

public sealed class UserExcludedEntityRepository(AnalyticsDbContext context) : EfRepository<UserExcludedEntity, Guid>(context), IUserExcludedEntityRepository
{
    public async Task<IReadOnlyList<UserExcludedEntity>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.UserExcludedEntities
            .Where(x => x.UserId == userId && x.ExpiresAt > now)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExcludedAsync(Guid userId, EntityType entityKind, Guid entityId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.UserExcludedEntities
            .AnyAsync(x => x.UserId == userId && x.EntityKind == entityKind && x.EntityId == entityId && x.ExpiresAt > now, ct)
            .ConfigureAwait(false);
    }
}
