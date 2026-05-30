using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class RefundPolicyRepository(BookingDbContext context)
    : EfRepository<RefundPolicy, Guid>(context), IRefundPolicyRepository
{
    public Task<RefundPolicy?> GetDefaultAsync(CancellationToken ct = default)
        => context.RefundPolicies.FirstOrDefaultAsync(p => p.IsDefault && p.IsActive, ct);

    public async Task<IReadOnlyList<RefundPolicy>> GetAllActiveAsync(CancellationToken ct = default)
        => await context.RefundPolicies.Where(p => p.IsActive).ToListAsync(ct);
}
