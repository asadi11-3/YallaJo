using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class CommissionSnapshotRepository(BookingDbContext context)
    : EfRepository<CommissionSnapshot, Guid>(context), ICommissionSnapshotRepository
{
    public Task<CommissionSnapshot?> GetActiveByTierAsync(
        string tier,
        string currency,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tier);
        ArgumentNullException.ThrowIfNull(currency);

        var normalizedTier = tier.Trim();
        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        return context.CommissionSnapshots
            .AsNoTracking()
            .Where(s => s.IsActive
                        && s.Tier == normalizedTier
                        && s.Currency == normalizedCurrency)
            .OrderByDescending(s => s.LastUpdatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
