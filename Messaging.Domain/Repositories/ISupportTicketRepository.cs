using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

public interface ISupportTicketRepository : IRepository<SupportTicket, Guid>
{
    Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken ct = default);
}
