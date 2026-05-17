using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class UserInteractionRepository(AnalyticsDbContext context)
    : EfRepository<UserInteraction, long>(context), IUserInteractionRepository
{
    public async Task<IReadOnlyList<UserInteraction>> GetByUserIdAsync(Guid userId, int take, CancellationToken ct = default)
        => await context.UserInteractions
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.OccurredAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserInteraction>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await context.UserInteractions
            .Where(i => i.EntityType == entityType && i.EntityId == entityId)
            .ToListAsync(ct);
}
