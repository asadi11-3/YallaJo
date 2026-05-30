using Finance.Contracts.Services;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Finance.Infrastructure.Services;

internal sealed class GuideEarningReader(FinanceDbContext context) : IGuideEarningReader
{
    public async Task<GuideEarningsSummary> GetSummaryAsync(Guid guideUserId, CancellationToken ct = default)
    {
        var payouts = await context.Payouts
            .AsNoTracking()
            .Where(p => p.ProviderId == guideUserId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var completed = payouts.Where(p => p.Status == PayoutStatus.Completed).ToList();
        var pending = payouts.Where(p => p.Status is PayoutStatus.Pending or PayoutStatus.ReadyForPayout or PayoutStatus.Hold).ToList();

        var totalGross = completed.Sum(p => p.GrossAmount.Amount);
        var totalCommission = completed.Sum(p => p.CommissionAmount.Amount);
        var netEarnings = completed.Sum(p => p.NetAmount.Amount);
        var thisMonth = completed
            .Where(p => p.CompletedAt >= thisMonthStart || (p.CompletedAt is null && p.BatchPeriodEnd >= DateOnly.FromDateTime(thisMonthStart)))
            .Sum(p => p.NetAmount.Amount);
        var pendingPayout = pending.Sum(p => p.NetAmount.Amount);
        var currency = payouts.FirstOrDefault()?.Currency ?? "JOD";

        return new GuideEarningsSummary(totalGross, thisMonth, pendingPayout, totalCommission, netEarnings, currency);
    }

    public async Task<IReadOnlyList<GuideEarningByTour>> GetByTourAsync(Guid guideUserId, CancellationToken ct = default)
    {
        var rows = await context.PayoutItems
            .AsNoTracking()
            .Where(i => i.Payout.ProviderId == guideUserId)
            .GroupBy(i => i.BookingId)
            .Select(g => new
            {
                BookingId = g.Key,
                BookingCount = g.Count(),
                GrossAmount = g.Sum(i => i.GrossAmount.Amount),
                CommissionAmount = g.Sum(i => i.CommissionAmount.Amount),
                NetAmount = g.Sum(i => i.NetAmount.Amount),
                Currency = g.Select(i => i.Payout.Currency).FirstOrDefault() ?? "JOD"
            })
            .OrderByDescending(x => x.NetAmount)
            .ToListAsync(ct);

        return rows
            .Select(row => new GuideEarningByTour(
                row.BookingId,
                $"Booking {row.BookingId.ToString("N")[..8]}",
                row.BookingCount,
                row.GrossAmount,
                row.CommissionAmount,
                row.NetAmount,
                row.Currency))
            .ToList();
    }

    public async Task<PaginatedResult<GuideEarningHistoryItem>> GetHistoryAsync(Guid guideUserId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.PayoutItems
            .AsNoTracking()
            .Where(i => i.Payout.ProviderId == guideUserId)
            .OrderByDescending(i => i.Payout.CompletedAt ?? i.Payout.UpdatedAt ?? i.Payout.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new GuideEarningHistoryItem(
                i.Id,
                i.BookingId,
                i.Payout.CompletedAt.HasValue ? DateOnly.FromDateTime(i.Payout.CompletedAt.Value) : i.Payout.BatchPeriodEnd,
                i.GrossAmount.Amount,
                i.CommissionAmount.Amount,
                i.NetAmount.Amount,
                i.Payout.Currency,
                i.Payout.Status.ToString()))
            .ToListAsync(ct);

        return new PaginatedResult<GuideEarningHistoryItem>(items, totalCount, page, pageSize);
    }
}
