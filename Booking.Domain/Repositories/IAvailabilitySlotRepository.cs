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

    Task<Guid?> GetTourGuideIdByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<bool> AnyOverlapAsync(
        Guid tourId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly? cursorDate,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourOnDateAsync(
        Guid tourId,
        DateOnly date,
        CancellationToken ct = default);

    Task<IReadOnlyList<AvailabilitySlot>> GetActiveSlotsForTourInRangeAsync(
        Guid tourId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default);

    Task<int> CountActiveSlotsForTourAsync(
        Guid tourId,
        DateOnly fromDate,
        CancellationToken ct = default);
}
