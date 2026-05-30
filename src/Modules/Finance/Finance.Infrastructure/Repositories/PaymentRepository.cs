using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class PaymentRepository(FinanceDbContext context)
    : EfRepository<Payment, Guid>(context), IPaymentRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken ct = default)
        => _context.Payments
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == transactionId, ct);

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _context.Payments
            .AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Payment> Items, Guid? NextCursor)> GetMyPaymentsPageAsync(
        Guid userId,
        Guid? afterId,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == userId);

        if (afterId is not null)
        {
            query = query.Where(p => p.Id.CompareTo(afterId.Value) < 0);
        }

        var items = await query
            .OrderByDescending(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        Guid? nextCursor = null;
        if (items.Count > pageSize)
        {
            nextCursor = items[pageSize - 1].Id;
            items.RemoveAt(pageSize);
        }

        return (items, nextCursor);
    }

    public async Task<(IReadOnlyList<Payment> Items, Guid? NextCursor, int? TotalCount)> GetAdminPageAsync(
        Guid? userId,
        Guid? providerId,
        PaymentStatus? status,
        PaymentType? type,
        Guid? afterId,
        int pageSize,
        bool countTotal,
        CancellationToken ct = default)
    {
        var baseQuery = _context.Payments.AsNoTracking();

        if (userId is not null)
        {
            baseQuery = baseQuery.Where(p => p.UserId == userId.Value);
        }

        if (providerId is not null)
        {
            baseQuery = baseQuery.Where(p => p.ProviderId == providerId.Value);
        }

        if (status is not null)
        {
            baseQuery = baseQuery.Where(p => p.Status == status.Value);
        }

        if (type is not null)
        {
            baseQuery = baseQuery.Where(p => p.PaymentType == type.Value);
        }

        int? totalCount = null;
        if (countTotal)
        {
            totalCount = await baseQuery.CountAsync(ct);
        }

        var pagedQuery = baseQuery;
        if (afterId is not null)
        {
            pagedQuery = pagedQuery.Where(p => p.Id.CompareTo(afterId.Value) < 0);
        }

        var items = await pagedQuery
            .OrderByDescending(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        Guid? nextCursor = null;
        if (items.Count > pageSize)
        {
            nextCursor = items[pageSize - 1].Id;
            items.RemoveAt(pageSize);
        }

        return (items, nextCursor, totalCount);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingRefundsOlderThanAsync(
        DateTime olderThanUtc,
        int maxRetries,
        int batchSize,
        CancellationToken ct = default)
    {
        var clampedBatch = Math.Clamp(batchSize, 1, 1000);

        return await _context.Payments
            .Where(p => p.PaymentType == PaymentType.Refund
                && p.Status == PaymentStatus.Failed
                && p.RetryCount < maxRetries
                && p.UpdatedAt < olderThanUtc)
            .OrderBy(p => p.UpdatedAt)
            .Take(clampedBatch)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payment>> GetEscrowReleaseEligibleAsync(
        DateTime asOfUtc,
        CancellationToken ct = default)
    {
        // Booking-type completed payments with escrow window elapsed, not yet linked to a PayoutItem.
        // We do a NOT EXISTS subquery against PayoutItems on (BookingId).
        var payouts = _context.Set<PayoutItem>().Select(pi => pi.BookingId);

        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentType == PaymentType.Booking
                && p.Status == PaymentStatus.Completed
                && p.EscrowReleaseEligibleAt != null
                && p.EscrowReleaseEligibleAt <= asOfUtc
                && p.BookingId != null
                && !payouts.Contains(p.BookingId.Value))
            .OrderBy(p => p.EscrowReleaseEligibleAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payment>> GetCompletedProviderPaymentsAsync(
        Guid providerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.ProviderId == providerId && p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Completed);

        if (fromUtc is not null)
        {
            query = query.Where(p => p.PaidAt >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(p => p.PaidAt <= toUtc.Value);
        }

        return await query.OrderByDescending(p => p.PaidAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payment>> GetCompletedPaymentsAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Completed);

        if (fromUtc is not null)
        {
            query = query.Where(p => p.PaidAt >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(p => p.PaidAt <= toUtc.Value);
        }

        return await query.OrderByDescending(p => p.PaidAt).ToListAsync(ct);
    }
}
