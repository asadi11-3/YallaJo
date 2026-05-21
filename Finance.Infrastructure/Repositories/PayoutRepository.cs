using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class PayoutRepository(FinanceDbContext context)
    : EfRepository<Payout, Guid>(context), IPayoutRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<(IReadOnlyList<Payout> Items, Guid? NextCursor)> GetPendingApprovalAsync(
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default)
    {
        var size = pageSize;
        var q = _context.Payouts
            .AsNoTracking()
            .Where(p => p.Status == PayoutStatus.Pending || p.Status == PayoutStatus.Hold);

        if (afterId.HasValue)
        {
            q = q.Where(p => p.Id.CompareTo(afterId.Value) < 0);
        }

        var items = await q.OrderByDescending(p => p.Id).Take(size + 1).ToListAsync(ct);
        Guid? next = items.Count > size ? items[size - 1].Id : null;
        if (items.Count > size)
        {
            items.RemoveAt(size);
        }

        return (items, next);
    }

    public async Task<(IReadOnlyList<Payout> Items, Guid? NextCursor)> GetByProviderAsync(
        Guid providerId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default)
    {
        var size = pageSize;
        var q = _context.Payouts
            .AsNoTracking()
            .Where(p => p.ProviderId == providerId);

        if (afterId.HasValue)
        {
            q = q.Where(p => p.Id.CompareTo(afterId.Value) < 0);
        }

        var items = await q.OrderByDescending(p => p.Id).Take(size + 1).ToListAsync(ct);
        Guid? next = items.Count > size ? items[size - 1].Id : null;
        if (items.Count > size)
        {
            items.RemoveAt(size);
        }

        return (items, next);
    }

    public async Task<IReadOnlyList<Payout>> GetByPeriodAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct = default)
        => await _context.Payouts
            .AsNoTracking()
            .Where(p => p.BatchPeriodStart == periodStart && p.BatchPeriodEnd == periodEnd)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct = default)
        => await _context.Payouts
            .AsNoTracking()
            .Where(p => p.Status == PayoutStatus.Pending)
            .ToListAsync(ct);
}
