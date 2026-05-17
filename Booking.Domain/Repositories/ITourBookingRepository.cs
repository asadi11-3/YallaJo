using Booking.Domain.Entities;
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
}
