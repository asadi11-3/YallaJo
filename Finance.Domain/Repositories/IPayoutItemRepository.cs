using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

/// <summary>
/// Read-side queries for <see cref="PayoutItem"/> entity (child of Payout aggregate).
/// </summary>
/// <remarks>
/// PayoutItem is NOT an aggregate root; it is owned by Payout. This interface intentionally does
/// not inherit from <c>IRepository&lt;PayoutItem, Guid&gt;</c> because writes must go through the
/// owning Payout aggregate. Only read queries are exposed here.
/// </remarks>
public interface IPayoutItemRepository
{
    /// <summary>Returns all payout items belonging to the given payout, ordered by CreatedAt asc.</summary>
    Task<IReadOnlyList<PayoutItem>> GetByPayoutIdAsync(Guid payoutId, CancellationToken ct = default);
}
