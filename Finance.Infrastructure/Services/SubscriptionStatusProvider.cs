using Finance.Contracts.Services;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Services;

internal sealed class SubscriptionStatusProvider(FinanceDbContext dbContext) : ISubscriptionStatusProvider
{
    public Task<bool> HasActiveSubscriptionAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var tierName = tier.ToString();

        return dbContext.Subscriptions
            .AsNoTracking()
            .AnyAsync(subscription =>
                subscription.UserId == userId &&
                subscription.Status == SubscriptionStatus.Active &&
                subscription.SubscriptionPlan.Name == tierName &&
                (subscription.ActivePeriod == null || subscription.ActivePeriod.Contains(now)),
                ct);
    }
}
