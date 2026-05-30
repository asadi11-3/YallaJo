using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

/// <summary>
/// Repository for Finance-local PaymentExpectation snapshots seeded from Booking events.
/// Plain query/store interface (not IAggregateRoot since PaymentExpectation is BaseEntity).
/// </summary>
public interface IPaymentExpectationRepository
{
    Task<PaymentExpectation?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task AddAsync(PaymentExpectation expectation, CancellationToken ct = default);
    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default);
}
