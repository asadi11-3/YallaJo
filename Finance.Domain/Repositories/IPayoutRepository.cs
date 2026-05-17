using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IPayoutRepository : IRepository<Payout, Guid>
{
    Task<IReadOnlyList<Payout>> GetByRecipientUserIdAsync(Guid recipientUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct = default);
}
