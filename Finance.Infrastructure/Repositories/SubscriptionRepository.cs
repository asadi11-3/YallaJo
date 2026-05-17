using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class SubscriptionRepository(FinanceDbContext context)
    : EfRepository<Subscription, Guid>(context), ISubscriptionRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active, ct);

    public async Task<IReadOnlyList<Subscription>> GetByPlanIdAsync(Guid planId, CancellationToken ct = default)
        => await _context.Subscriptions.Where(s => s.PlanId == planId).ToListAsync(ct);

    public async Task<IReadOnlyList<Subscription>> GetExpiringAsync(DateTime threshold, CancellationToken ct = default)
        => await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                     && s.ActivePeriod != null
                     && s.ActivePeriod.End != null
                     && s.ActivePeriod.End < threshold)
            .ToListAsync(ct);
}
