using Finance.Domain.Entities;
using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IDisputeRepository : IRepository<Dispute, Guid>
{
    Task<IReadOnlyList<Dispute>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<IReadOnlyList<Dispute>> GetOpenDisputesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Dispute>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Per-status dispute counts computed in a single grouped query.
    /// Powers the admin queue counted tabs.
    /// </summary>
    Task<IReadOnlyDictionary<DisputeStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
}
