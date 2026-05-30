using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IDisputeRepository : IRepository<Dispute, Guid>
{
    Task<IReadOnlyList<Dispute>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<IReadOnlyList<Dispute>> GetOpenDisputesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Dispute>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
