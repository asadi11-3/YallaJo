using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

/// <summary>
/// Read-side queries for <see cref="CommissionRule"/> aggregate.
/// </summary>
/// <remarks>
/// Method shapes target the post-T6 schema (Tier + MonthlyRevenue bracket).
/// PW-5 implementation falls back to the current (Name + Amount-bracket) schema until T6 reshapes the entity.
/// </remarks>
public interface ICommissionRuleRepository : IRepository<CommissionRule, Guid>
{
    /// <summary>Returns the active rule matching the given tier and currency, or null if none.</summary>
    Task<CommissionRule?> GetForTierAsync(string tier, string currency, CancellationToken ct = default);

    /// <summary>Returns rules whose monthly-revenue range overlaps [min, max] for the given tier+currency.</summary>
    Task<IReadOnlyList<CommissionRule>> GetOverlappingAsync(
        string tier,
        string currency,
        decimal minMonthlyRevenue,
        decimal? maxMonthlyRevenue,
        CancellationToken ct = default);

    /// <summary>Returns all active rules (IsActive = true).</summary>
    Task<IReadOnlyList<CommissionRule>> GetActiveAsync(CancellationToken ct = default);
}
