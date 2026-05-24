using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

public interface IRefundPolicyRepository : IRepository<RefundPolicy, Guid>
{
    Task<RefundPolicy?> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);

    Task<RefundPolicy?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
}
