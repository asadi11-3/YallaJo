using Booking.Domain.Entities;
using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

/// <summary>
/// Repository contract for <see cref="TourBooking"/> aggregates.
/// Inherits the generic CRUD surface (GetByIdAsync, GetAllAsync, AddAsync, etc.)
/// from <see cref="IRepository{TEntity,TKey}"/>.
/// Domain-specific include patterns are isolated in the EF impl.
/// </summary>
public interface ITourBookingRepository : IRepository<TourBooking, Guid>
{
    /// <summary>Loads the booking with all related details (Tour, Participants, JoinRequests, Payment).</summary>
    Task<TourBooking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists bookings owned by a specific user.</summary>
    Task<IReadOnlyList<TourBooking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lists bookings for a specific tour.</summary>
    Task<IReadOnlyList<TourBooking>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the user already has any non-cancelled, non-rejected booking
    /// for the given tour on the given calendar date. Used by POST /tour Step 1
    /// to prevent same-tour-same-date duplicates.
    /// </summary>
    Task<bool> HasActiveBookingForTourOnDateAsync(
        Guid userId,
        Guid tourId,
        DateOnly slotDate,
        CancellationToken ct = default);

    /// <summary>
    /// Counts the user's bookings currently in AwaitingPayment state.
    /// Used by POST /tour Step 1 to enforce the 3-max concurrent unpaid bookings rule.
    /// </summary>
    Task<int> CountActiveAwaitingPaymentByUserAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Looks up a booking by its public reference (e.g. "YJ-20260701-A7X3K9").
    /// </summary>
    Task<TourBooking?> GetByReferenceAsync(
        string reference,
        CancellationToken ct = default);

    /// <summary>
    /// Cursor-paginated query for a user's tour bookings (B-R10).
    /// Filters:
    ///   - statuses (any-match if non-empty)
    ///   - fromDate/toDate (against slot date via AvailabilitySlot join)
    ///   - tourId (exact match)
    /// Sort order: CreatedAt DESC, Id ASC.
    /// Cursor filter: rows strictly AFTER the cursor in that order.
    /// Returns up to <paramref name="limit"/> rows.
    /// Callers typically pass limit = pageSize + 1 to detect if there is a next page.
    /// </summary>
    Task<IReadOnlyList<TourBooking>> QueryUserBookingsAsync(
        Guid userId,
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? tourId,
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default);

    /// <summary>
    /// Optional total count for a user's filtered bookings.
    /// Same filter shape as <see cref="QueryUserBookingsAsync"/>.
    /// </summary>
    Task<int> CountUserBookingsAsync(
        Guid userId,
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? tourId,
        CancellationToken ct = default);

    /// <summary>
    /// Admin: cursor-paginated query across ALL bookings.
    /// Same shape as <see cref="QueryUserBookingsAsync"/> but with no implicit user scope
    /// and additional filters for userId/providerId/paymentStatus.
    /// </summary>
    /// <remarks>
    /// <paramref name="paymentStatus"/> is accepted as a hint for the time being; the actual
    /// payment projection lives in the Finance module and is not yet wired across modules.
    /// Implementations may log + ignore the filter until Finance integration ships.
    /// </remarks>
    Task<IReadOnlyList<TourBooking>> QueryAllBookingsAsync(
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        Guid? providerId,
        Guid? tourId,
        string? paymentStatus,
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: total count of bookings matching the same filter shape as
    /// <see cref="QueryAllBookingsAsync"/>.
    /// </summary>
    Task<int> CountAllBookingsAsync(
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        Guid? providerId,
        Guid? tourId,
        string? paymentStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Lists provider-confirmation bookings older than the configured auto-accept cutoff.</summary>
    Task<IReadOnlyList<TourBooking>> GetPendingConfirmationOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct = default);
}
