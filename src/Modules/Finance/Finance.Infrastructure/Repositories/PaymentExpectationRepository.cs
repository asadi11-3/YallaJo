using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

internal sealed class PaymentExpectationRepository(FinanceDbContext context) : IPaymentExpectationRepository
{
    private readonly FinanceDbContext _context = context;

    public Task<PaymentExpectation?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => _context.PaymentExpectations
            .FirstOrDefaultAsync(e => e.BookingId == bookingId, ct);

    public async Task AddAsync(PaymentExpectation expectation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expectation);
        await _context.PaymentExpectations.AddAsync(expectation, ct).ConfigureAwait(false);
    }

    public Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default)
        => _context.PaymentExpectations
            .AsNoTracking()
            .AnyAsync(e => e.BookingId == bookingId, ct);
}
