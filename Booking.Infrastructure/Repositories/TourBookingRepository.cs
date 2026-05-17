using Booking.Domain.Entities;
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
}
