using Booking.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Booking.Domain.Repositories;

public interface ICommissionSnapshotRepository : IRepository<CommissionSnapshot, Guid>
{
    Task<CommissionSnapshot?> GetActiveByTierAsync(string tier, string currency, CancellationToken ct = default);
}
