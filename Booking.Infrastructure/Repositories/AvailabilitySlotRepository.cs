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
}
