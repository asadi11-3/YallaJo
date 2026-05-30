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

    public Task<Guid?> GetTourGuideIdByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return context.TourGuides
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.IsActive)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> AnyOverlapAsync(
        Guid tourId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeId,
        CancellationToken ct = default)
        => AnyAsync(
            s => s.TourId == tourId
                 && s.Date == date
                 && s.IsActive
                 && !(s.EndTime <= startTime || s.StartTime >= endTime)
                 && (excludeId == null || s.Id != excludeId.Value),
            ct);

    public async Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly? cursorDate,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default)
    {
        var query = context.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.TourId == tourId && s.IsActive && s.Date >= fromDate);

        if (cursorDate.HasValue && cursorId.HasValue)
        {
            var anchorDate = cursorDate.Value;
            var anchorId = cursorId.Value;
            query = query.Where(s => s.Date > anchorDate
                                     || (s.Date == anchorDate && s.Id.CompareTo(anchorId) > 0));
        }

        return await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourOnDateAsync(
        Guid tourId,
        DateOnly date,
        CancellationToken ct = default)
        => await GetAllAsync(
            filter: s => s.TourId == tourId && s.IsActive && s.Date == date,
            orderBy: q => q.OrderBy(s => s.StartTime).ThenBy(s => s.Id),
            asNoTracking: true,
            ct: ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourInRangeAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
        => await GetAllAsync(
            filter: s => s.TourId == tourId
                         && s.IsActive
                         && s.Date >= fromDate
                         && s.Date <= toDate,
            orderBy: q => q.OrderBy(s => s.Date).ThenBy(s => s.StartTime),
            asNoTracking: true,
            ct: ct).ConfigureAwait(false);

    public Task<int> CountActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        CancellationToken ct = default)
        => CountAsync(
            s => s.TourId == tourId && s.IsActive && s.Date >= fromDate,
            ct);

    public async Task<HashSet<DateOnly>> GetExistingSlotDatesAsync(
        Guid tourGuideId,
        Guid tourId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        var dates = await context.AvailabilitySlots
            .Where(s => s.TourGuideId == tourGuideId && s.TourId == tourId && s.Date >= fromDate && s.Date <= toDate)
            .Select(s => s.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. dates];
    }

    public async Task<IReadOnlyList<AvailabilitySlot>> GetInactivePastSlotsAsync(
        DateOnly before,
        int batchSize,
        CancellationToken ct = default)
        => await context.AvailabilitySlots
            .Where(s => s.Date < before && s.BookedCount == 0 && s.LockedCount == 0 && !s.IsActive)
            .OrderBy(s => s.Date)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
