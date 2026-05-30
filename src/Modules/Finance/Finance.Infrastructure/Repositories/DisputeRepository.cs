using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class DisputeRepository(FinanceDbContext context)
    : EfRepository<Dispute, Guid>(context), IDisputeRepository
{
    private readonly FinanceDbContext _context = context;

    public async Task<IReadOnlyList<Dispute>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        => await _context.Disputes.AsNoTracking().Where(d => d.PaymentId == paymentId).ToListAsync(ct);

    public async Task<IReadOnlyList<Dispute>> GetOpenDisputesAsync(CancellationToken ct = default)
        => await _context.Disputes
            .AsNoTracking()
            .Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview || d.Status == DisputeStatus.Escalated)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Dispute>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Disputes
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
}
