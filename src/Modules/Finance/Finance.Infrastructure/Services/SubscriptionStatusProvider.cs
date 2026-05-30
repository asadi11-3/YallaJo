using Finance.Contracts.Services;
using Finance.Domain.Enums;

namespace Finance.Infrastructure.Services;

internal sealed class SubscriptionStatusProvider : ISubscriptionStatusProvider
{
    // Deferred post-MVP: Subscription table removed. Returns default (no active subscription).
    public Task<bool> HasActiveSubscriptionAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default)
        => Task.FromResult(false);
}
