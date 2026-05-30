namespace Booking.Application.Interfaces;

/// <summary>
/// Unit-of-work facade for the Booking bounded context.
/// Delegates to <see cref="YallaJo.SharedKernel.Infrastructure.Data.IUnitOfWork{BookingDbContext}"/>
/// so domain-event dispatch is guaranteed before every commit.
/// </summary>
public interface IBookingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
