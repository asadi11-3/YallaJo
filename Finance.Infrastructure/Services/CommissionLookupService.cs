using Finance.Contracts.Services;
using Finance.Domain.Repositories;

namespace Finance.Infrastructure.Services;

/// <summary>
/// Resolves the booking commission rule from the active Finance rule catalog,
/// falling back to the Phase 3 default when no rule is configured.
/// </summary>
internal sealed class CommissionLookupService(ICommissionRuleRepository commissionRuleRepository) : ICommissionLookupService
{
    private const string DefaultTier = "Free";
    private const string DefaultCurrency = "JOD";
    private const decimal FallbackRate = 0.15m;

    public async Task<CommissionResult> GetCommissionAsync(Guid tourId, CancellationToken ct = default)
    {
        // TODO(Phase 3): this public contract only provides tourId, but commission lookup is a
        // booking-time/provider question. Refactor the signature to accept (Guid providerId,
        // string currency), then resolve the provider subscription tier before querying rules.
        var tier = await GetProviderTierOverrideAsync(tourId, ct).ConfigureAwait(false) ?? DefaultTier;
        var rule = await commissionRuleRepository
            .GetForTierAsync(tier, DefaultCurrency, ct)
            .ConfigureAwait(false);

        return rule is not null
            ? new CommissionResult(rule.Percentage / 100m, 0m, decimal.MaxValue)
            : new CommissionResult(FallbackRate, 0m, decimal.MaxValue);
    }

    private static Task<string?> GetProviderTierOverrideAsync(Guid tourId, CancellationToken ct)
    {
        _ = tourId;
        _ = ct;
        return Task.FromResult<string?>(null);
    }
}
