using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="AvailabilitySlot"/> aggregates.
/// Concurrency on BookedCount/LockedCount/MaxCapacity is enforced via the inherited
/// <c>RowVersion</c> token from <c>AuditableEntity</c> (optimistic concurrency).
/// </summary>
public interface IAvailabilitySlotRepository : IRepository<AvailabilitySlot, Guid>
{
    /// <summary>
    /// Reloads the slot with its row-version for the booking engine's optimistic capacity update.
    /// Concurrency conflicts surface as DbUpdateConcurrencyException -> Result conflict in handlers.
    /// </summary>
    Task<AvailabilitySlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists slots for a specific tour (no filtering, used by housekeeping paths).</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);

    /// <summary>Lists slots owned by a specific tour guide.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetByTourGuideIdAsync(Guid tourGuideId, CancellationToken ct = default);

    /// <summary>
    /// Resolves the <c>TourGuide.Id</c> for the given application user, if one exists.
    /// Used by availability-slot ownership checks where the provider's owner user must
    /// map to a TourGuide aggregate before slots can be created/updated.
    /// </summary>
    Task<Guid?> GetTourGuideIdByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if any active slot for the tour overlaps the given [startTime, endTime]
    /// window on the given date. <paramref name="excludeId"/> can be used to ignore the slot
    /// being updated.
    /// </summary>
    Task<bool> AnyOverlapAsync(
        Guid tourId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeId,
        CancellationToken ct = default);

    /// <summary>
    /// Cursor-paginated query for active slots of a tour, ordered by (Date, Id).
    /// Used by GET /availability/{tourId}.
    /// </summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly? cursorDate,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default);

    /// <summary>Lists active slots for a tour on a specific date.</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourOnDateAsync(
        Guid tourId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>Lists active slots for a tour over a date range (inclusive).</summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourInRangeAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);

    /// <summary>Returns the count of active slots for a tour from <paramref name="fromDate"/> onward.</summary>
    Task<int> CountActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns existing slot dates for a given (TourGuideId, TourId) within a date range.
    /// Used by SlotGenerationService to skip already-generated dates.
    /// </summary>
    Task<HashSet<DateOnly>> GetExistingSlotDatesAsync(
        Guid tourGuideId,
        Guid tourId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns inactive past slots with zero bookings, suitable for cleanup.
    /// Used by SlotCleanupService.
    /// </summary>
    Task<IReadOnlyList<AvailabilitySlot>> GetInactivePastSlotsAsync(
        DateOnly before,
        int batchSize,
        CancellationToken ct = default);
}
