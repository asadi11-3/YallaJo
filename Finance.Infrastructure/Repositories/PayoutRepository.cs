using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class PayoutRepository(FinanceDbContext context)
    : EfRepository<Payout, Guid>(context), IPayoutRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<IReadOnlyList<Payout>> GetByRecipientUserIdAsync(Guid recipientUserId, CancellationToken ct = default)
        => await _context.Payouts.Where(p => p.RecipientUserId == recipientUserId).ToListAsync(ct);

    public async Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct = default)
        => await _context.Payouts.Where(p => p.Status == PayoutStatus.Pending).ToListAsync(ct);
}
