using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Messaging.Infrastructure.Repositories;

internal sealed class SupportTicketRepository(MessagingDbContext context)
    : EfRepository<SupportTicket, Guid>(context), ISupportTicketRepository
{
    public Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
        => context.SupportTickets
            .Include(t => t.TicketMessages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<IReadOnlyList<SupportTicket>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<SupportTicket>)t.Result, ct);

    public Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken ct = default)
        => context.SupportTickets
            .Where(t => t.Status == status)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<SupportTicket>)t.Result, ct);
}
