using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Booking.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="TourBooking"/> aggregates.
/// Delegates generic CRUD to <see cref="EfRepository{TEntity,TKey}"/>.
/// </summary>
internal sealed class TourBookingRepository(BookingDbContext context)
    : EfRepository<TourBooking, Guid>(context), ITourBookingRepository
{
    public Task<TourBooking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => context.TourBookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<TourBooking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.TourBookings.Where(b => b.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<TourBooking>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default)
        => await context.TourBookings.Where(b => b.TourId == tourId).ToListAsync(ct);

    /// <summary>
    /// Returns true if the user already has a non-cancelled, non-rejected booking
    /// for the given tour on the given slot date. Enforces B-R4 (anti-double-booking).
    /// </summary>
    public async Task<bool> HasActiveBookingForTourOnDateAsync(
        Guid userId,
        Guid tourId,
        DateOnly slotDate,
        CancellationToken ct = default)
    {
        var slotIdsOnDate = context.AvailabilitySlots
            .Where(s => s.TourId == tourId && s.Date == slotDate)
            .Select(s => s.Id);

        return await context.TourBookings
            .AnyAsync(
                b => b.UserId == userId
                    && b.TourId == tourId
                    && b.Status != BookingStatus.Cancelled
                    && b.Status != BookingStatus.Rejected
                    && slotIdsOnDate.Contains(b.AvailabilitySlotId),
                ct);
    }

    /// <summary>
    /// Counts the user's currently-unpaid (AwaitingPayment) bookings.
    /// Enforces B-R4 concurrent-limit cap of 3.
    /// </summary>
    public Task<int> CountActiveAwaitingPaymentByUserAsync(Guid userId, CancellationToken ct = default)
        => context.TourBookings
            .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.AwaitingPayment, ct);

    /// <summary>
    /// Lookup by human-readable reference code (e.g., <c>YJ-20260615-A7X3K9</c>).
    /// Unique-indexed.
    /// </summary>
    public Task<TourBooking?> GetByReferenceAsync(string reference, CancellationToken ct = default)
        => context.TourBookings.FirstOrDefaultAsync(b => b.Reference == reference, ct);

    /// <summary>
    /// Cursor-paginated query for a user's tour bookings (B-R10).
    /// Order: CreatedAt DESC, Id ASC.
    /// Cursor filter (next page only): rows whose (CreatedAt, Id) come strictly after the cursor's
    /// (CreatedAt, Id) in the order above — i.e. CreatedAt &lt; cursor.CreatedAt OR
    /// (CreatedAt == cursor.CreatedAt AND Id &gt; cursor.Id).
    /// </summary>
    public async Task<IReadOnlyList<TourBooking>> QueryUserBookingsAsync(
        Guid userId,
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? tourId,
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken ct = default)
    {
        var query = BuildUserBookingsQuery(userId, statuses, fromDate, toDate, tourId);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            var cursorAt = cursorCreatedAt.Value;
            var cursorBookingId = cursorId.Value;
            query = query.Where(
                b => b.CreatedAt < cursorAt
                    || (b.CreatedAt == cursorAt && b.Id.CompareTo(cursorBookingId) > 0));
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Counts a user's bookings matching the same filter shape as <see cref="QueryUserBookingsAsync"/>.
    /// </summary>
    public async Task<int> CountUserBookingsAsync(
        Guid userId,
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? tourId,
        CancellationToken ct = default)
    {
        var query = BuildUserBookingsQuery(userId, statuses, fromDate, toDate, tourId);
        return await query.CountAsync(ct).ConfigureAwait(false);
    }

    private IQueryable<TourBooking> BuildUserBookingsQuery(
        Guid userId,
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? tourId)
    {
        var query = context.TourBookings.AsNoTracking().Where(b => b.UserId == userId);

        if (statuses is { Count: > 0 })
        {
            var statusList = statuses.Distinct().ToArray();
            query = query.Where(b => statusList.Contains(b.Status));
        }

        if (tourId.HasValue)
        {
            var tourFilter = tourId.Value;
            query = query.Where(b => b.TourId == tourFilter);
        }

        if (fromDate.HasValue || toDate.HasValue)
        {
            var from = fromDate;
            var to = toDate;
            var dateScopedSlotIds = context.AvailabilitySlots
                .Where(s => (!from.HasValue || s.Date >= from.Value)
                    && (!to.HasValue || s.Date <= to.Value))
                .Select(s => s.Id);
            query = query.Where(b => dateScopedSlotIds.Contains(b.AvailabilitySlotId));
        }

        return query;
    }

    /// <summary>
    /// Admin: cursor-paginated query across ALL bookings. Same sort order
    /// + cursor semantics as <see cref="QueryUserBookingsAsync"/>.
    /// </summary>
    public async Task<IReadOnlyList<TourBooking>> QueryAllBookingsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminBookingsQuery(statuses, fromDate, toDate, userId, providerId, tourId, paymentStatus);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            var cursorAt = cursorCreatedAt.Value;
            var cursorBookingId = cursorId.Value;
            query = query.Where(
                b => b.CreatedAt < cursorAt
                    || (b.CreatedAt == cursorAt && b.Id.CompareTo(cursorBookingId) > 0));
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Admin: total count of bookings matching the same filter shape as
    /// <see cref="QueryAllBookingsAsync"/>.
    /// </summary>
    public async Task<int> CountAllBookingsAsync(
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        Guid? providerId,
        Guid? tourId,
        string? paymentStatus,
        CancellationToken cancellationToken = default)
    {
        var query = BuildAdminBookingsQuery(statuses, fromDate, toDate, userId, providerId, tourId, paymentStatus);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<TourBooking> BuildAdminBookingsQuery(
        IReadOnlyList<BookingStatus>? statuses,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? userId,
        Guid? providerId,
        Guid? tourId,
        string? paymentStatus)
    {
        // paymentStatus is currently a no-op: Finance owns the payment projection
        // and a cross-module read model is not yet wired. The handler logs the
        // intent so we can revisit when Finance ships.
        _ = paymentStatus;

        var query = context.TourBookings.AsNoTracking();

        if (statuses is { Count: > 0 })
        {
            var statusList = statuses.Distinct().ToArray();
            query = query.Where(b => statusList.Contains(b.Status));
        }

        if (userId.HasValue)
        {
            var userFilter = userId.Value;
            query = query.Where(b => b.UserId == userFilter);
        }

        if (providerId.HasValue)
        {
            var providerFilter = providerId.Value;
            query = query.Where(b => b.ProviderId == providerFilter);
        }

        if (tourId.HasValue)
        {
            var tourFilter = tourId.Value;
            query = query.Where(b => b.TourId == tourFilter);
        }

        if (fromDate.HasValue || toDate.HasValue)
        {
            var from = fromDate;
            var to = toDate;
            var dateScopedSlotIds = context.AvailabilitySlots
                .Where(s => (!from.HasValue || s.Date >= from.Value)
                    && (!to.HasValue || s.Date <= to.Value))
                .Select(s => s.Id);
            query = query.Where(b => dateScopedSlotIds.Contains(b.AvailabilitySlotId));
        }

        return query;
    }
}
