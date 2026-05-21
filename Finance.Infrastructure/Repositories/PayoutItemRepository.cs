using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

internal sealed class PayoutItemRepository(FinanceDbContext context) : IPayoutItemRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<IReadOnlyList<PayoutItem>> GetByPayoutIdAsync(Guid payoutId, CancellationToken ct = default)
        => await _context.PayoutItems
            .AsNoTracking()
            .Where(pi => pi.PayoutId == payoutId)
            .OrderBy(pi => pi.CreatedAt)
            .ToListAsync(ct);
}
