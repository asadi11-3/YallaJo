using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Finance.Infrastructure.Repositories;

internal sealed class PaymentRepository(FinanceDbContext context)
    : EfRepository<Payment, Guid>(context), IPaymentRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken ct = default)
        => _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId, ct);

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Payments.Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _context.Payments.Where(p => p.BookingId == bookingId).ToListAsync(ct);
}
