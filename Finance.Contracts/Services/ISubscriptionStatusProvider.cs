namespace Finance.Contracts.Services;

public interface ISubscriptionStatusProvider
{
    Task<bool> HasActiveSubscriptionAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default);
}
