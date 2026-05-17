using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment, Guid>
{
    Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
}
