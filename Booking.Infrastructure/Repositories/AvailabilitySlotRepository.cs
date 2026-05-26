using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="AvailabilitySlot"/> aggregates.
/// Concurrency is enforced via the inherited <c>RowVersion</c> token from <c>AuditableEntity</c>.
/// </summary>
internal sealed class AvailabilitySlotRepository(BookingDbContext context)
    : EfRepository<AvailabilitySlot, Guid>(context), IAvailabilitySlotRepository
{
    public Task<AvailabilitySlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default)
        => context.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<AvailabilitySlot>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default)
        => await context.AvailabilitySlots.Where(s => s.TourId == tourId).ToListAsync(ct);

    public async Task<IReadOnlyList<AvailabilitySlot>> GetByTourGuideIdAsync(Guid tourGuideId, CancellationToken ct = default)
        => await context.AvailabilitySlots.Where(s => s.TourGuideId == tourGuideId).ToListAsync(ct);

    public async Task<HashSet<DateOnly>> GetExistingSlotDatesAsync(Guid tourGuideId, Guid tourId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var dates = await context.AvailabilitySlots
            .Where(s => s.TourGuideId == tourGuideId && s.TourId == tourId && s.Date >= fromDate && s.Date <= toDate)
            .Select(s => s.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. dates];
    }

    public async Task<IReadOnlyList<AvailabilitySlot>> GetInactivePastSlotsAsync(DateOnly before, int batchSize, CancellationToken ct = default)
        => await context.AvailabilitySlots
            .Where(s => s.Date < before && s.BookedCount == 0 && s.LockedCount == 0 && !s.IsActive)
            .OrderBy(s => s.Date)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
