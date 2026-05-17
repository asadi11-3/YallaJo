using Finance.Contracts.Services;

namespace Finance.Infrastructure.Services;

/// <summary>
/// Stub implementation — returns a flat 10% commission with no caps until the
/// Finance team wires the real rule engine in Wave 5 (PW-1 phase). The real
/// implementation will read commission rules from <c>FinanceDbContext.CommissionRules</c>.
/// </summary>
internal sealed class CommissionLookupService : ICommissionLookupService
{
    public Task<CommissionResult> GetCommissionAsync(Guid tourId, CancellationToken ct = default)
        => Task.FromResult(new CommissionResult(Rate: 0.10m, MinimumAmount: 0m, MaximumAmount: decimal.MaxValue));
}
