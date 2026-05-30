using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Domain.Repositories;

/// <summary>Query interface for <see cref="BookingEligibilitySnapshot"/> (plain — not IAggregateRoot).</summary>
public interface IBookingEligibilitySnapshotRepository
{
    /// <summary>Returns the snapshot for a (userId, targetType, targetId) triple, or null if none.</summary>
    Task<BookingEligibilitySnapshot?> GetAsync(
        Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>Upserts (insert or update) the snapshot row.</summary>
    Task UpsertAsync(BookingEligibilitySnapshot snapshot, CancellationToken ct = default);
}
