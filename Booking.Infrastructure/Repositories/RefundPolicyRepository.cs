using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class RefundPolicyRepository(BookingDbContext context)
    : EfRepository<RefundPolicy, Guid>(context), IRefundPolicyRepository
{
    public Task<RefundPolicy?> GetByTourIdAsync(Guid tourId, CancellationToken ct = default)
        => context.RefundPolicies
            .AsNoTracking()
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.TourId == tourId, ct);

    public Task<RefundPolicy?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => context.RefundPolicies
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
}
