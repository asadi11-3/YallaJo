using System.Threading;
using System.Threading.Tasks;

namespace Booking.Application.Interfaces;

public interface IBookingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
