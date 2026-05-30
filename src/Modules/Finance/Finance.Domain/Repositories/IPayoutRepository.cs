using Finance.Domain.Entities;
using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IPayoutRepository : IRepository<Payout, Guid>
{
    /// <summary>Admin view: payouts awaiting approval (status Pending or Hold).</summary>
    Task<(IReadOnlyList<Payout> Items, Guid? NextCursor)> GetPendingApprovalAsync(
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Provider self-view: payouts for one provider (all statuses).</summary>
    Task<(IReadOnlyList<Payout> Items, Guid? NextCursor)> GetByProviderAsync(
        Guid providerId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Internal: payouts scheduled in a given period (used by batching service idempotency).</summary>
    Task<IReadOnlyList<Payout>> GetByPeriodAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct = default);

    /// <summary>Legacy: payouts in Pending status (used by tests/dev only).</summary>
    Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct = default);
}
