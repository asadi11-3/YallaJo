using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

internal sealed class SlotLockRepository(BookingDbContext context)
    : EfRepository<SlotLock, Guid>(context), ISlotLockRepository
{
    public Task<SlotLock?> GetActiveByUserAndSlotAsync(Guid userId, Guid slotId, CancellationToken ct = default)
        => context.SlotLocks.FirstOrDefaultAsync(
            l => l.UserId == userId && l.AvailabilitySlotId == slotId && !l.IsReleased && l.ExpiresAt > DateTime.UtcNow,
            ct);

    public async Task<IReadOnlyList<SlotLock>> GetExpiredLocksAsync(DateTime now, CancellationToken ct = default)
        => await context.SlotLocks
            .Where(l => !l.IsReleased && l.ExpiresAt <= now)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SlotLock>> GetExpiredActiveAsync(DateTime nowUtc, int batchSize, CancellationToken ct = default)
        => await context.SlotLocks
            .Where(l => !l.IsReleased && l.ExpiresAt <= nowUtc)
            .OrderBy(l => l.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(ct);
}
