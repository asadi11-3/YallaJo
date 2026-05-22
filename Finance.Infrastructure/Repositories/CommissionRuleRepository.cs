using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

/// <summary>
/// EF-backed implementation of <see cref="ICommissionRuleRepository"/> (T6 reshape).
/// </summary>
internal sealed class CommissionRuleRepository(FinanceDbContext context)
    : EfRepository<CommissionRule, Guid>(context), ICommissionRuleRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<CommissionRule?> GetForTierAsync(string tier, string currency, CancellationToken ct = default)
    {
        var t = tier.Trim();
        var c = currency.ToUpperInvariant();
        return await _context.CommissionRules
            .AsNoTracking()
            .Where(r => r.Tier == t && r.Currency == c && r.IsActive)
            .OrderBy(r => r.MinMonthlyRevenue)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetOverlappingAsync(
        string tier,
        string currency,
        decimal minMonthlyRevenue,
        decimal? maxMonthlyRevenue,
        CancellationToken ct = default)
    {
        var t = tier.Trim();
        var c = currency.ToUpperInvariant();

        var query = _context.CommissionRules
            .AsNoTracking()
            .Where(r => r.Tier == t && r.Currency == c && r.IsActive);

        if (maxMonthlyRevenue.HasValue)
        {
            query = query.Where(r =>
                r.MinMonthlyRevenue < maxMonthlyRevenue.Value &&
                (!r.MaxMonthlyRevenue.HasValue || minMonthlyRevenue < r.MaxMonthlyRevenue.Value));
        }
        else
        {
            query = query.Where(r =>
                !r.MaxMonthlyRevenue.HasValue || minMonthlyRevenue < r.MaxMonthlyRevenue.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetActiveAsync(CancellationToken ct = default)
        => await _context.CommissionRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Tier).ThenBy(r => r.Currency).ThenBy(r => r.MinMonthlyRevenue)
            .ToListAsync(ct);
}
