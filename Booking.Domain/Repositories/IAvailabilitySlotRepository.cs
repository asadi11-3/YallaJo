using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="AvailabilitySlot"/> aggregates.
/// Concurrency on BookedCount/LockedCount is enforced via AvailabilitySlot.RowVersion
/// (optimistic concurrency token in AuditableEntity).
/// </summary>
public interface IAvailabilitySlotRepository : IRepository<AvailabilitySlot, Guid>
{
    /// <summary>
    /// Reloads the slot with its row-version for the booking engine's optimistic capacity update.
    /// Concurrency conflicts surface as DbUpdateConcurrencyException -> Result conflict in handlers.
    /// </summary>
    Task<AvailabilitySlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists slots for a specific tour.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);

    /// <summary>Lists slots owned by a specific tour guide.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetByTourGuideIdAsync(Guid tourGuideId, CancellationToken ct = default);

    /// <summary>
    /// Returns existing slot dates for a given (TourGuideId, TourId) within a date range.
    /// Used by SlotGenerationService to skip already-generated dates.
    /// </summary>
    Task<HashSet<DateOnly>> GetExistingSlotDatesAsync(Guid tourGuideId, Guid tourId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

    /// <summary>
    /// Returns inactive past slots with zero bookings, suitable for cleanup.
    /// Used by SlotCleanupService.
    /// </summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetInactivePastSlotsAsync(DateOnly before, int batchSize, CancellationToken ct = default);
}
