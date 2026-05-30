using Booking.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Booking.Infrastructure.Persistence;

/// <summary>
/// Thin facade that delegates to <see cref="IUnitOfWork{BookingDbContext}"/> so that
/// domain-event dispatch is guaranteed before every commit.
/// </summary>
internal sealed class BookingUnitOfWork(IUnitOfWork<BookingDbContext> inner) : IBookingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}
