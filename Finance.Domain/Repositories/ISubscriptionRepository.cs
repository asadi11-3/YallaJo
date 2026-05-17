using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription, Guid>
{
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<Subscription>> GetExpiringAsync(DateTime threshold, CancellationToken ct = default);
}
