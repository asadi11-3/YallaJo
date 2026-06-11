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
    private readonly MessagingDbContext _context = context;

    public Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
        => _context.SupportTickets
            .Include(t => t.TicketMessages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<SupportTicket> Items, Guid? NextCursor)> GetByUserPagedAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.SupportTickets.AsNoTracking()
            .Where(t => t.CreatedByUserId == userId);
        if (afterId.HasValue) query = query.Where(t => t.Id.CompareTo(afterId.Value) < 0);
        var rows = await query.OrderByDescending(t => t.Id).Take(pageSize + 1).ToListAsync(ct);
        Guid? next = rows.Count > pageSize ? rows[pageSize].Id : null;
        return ((IReadOnlyList<SupportTicket>)rows.Take(pageSize).ToList(), next);
    }

    public async Task<(IReadOnlyList<SupportTicket> Items, Guid? NextCursor)> GetByAdminPagedAsync(
        TicketStatus? status, TicketCategory? category, Guid? afterId, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.SupportTickets.AsNoTracking();
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (category.HasValue) query = query.Where(t => t.Category == category.Value);
        if (afterId.HasValue) query = query.Where(t => t.Id.CompareTo(afterId.Value) < 0);
        var rows = await query.OrderByDescending(t => t.Id).Take(pageSize + 1).ToListAsync(ct);
        Guid? next = rows.Count > pageSize ? rows[pageSize].Id : null;
        return ((IReadOnlyList<SupportTicket>)rows.Take(pageSize).ToList(), next);
    }

    public Task<SupportTicket?> GetUnassignedOldestAsync(CancellationToken ct = default)
        => _context.SupportTickets
            .Where(t => t.Status == TicketStatus.Open && t.AssignedToUserId == null)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<TicketStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
        => await _context.SupportTickets
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public Task<IReadOnlyList<SupportTicket>> GetOverdueSlaTicketsAsync(
        DateTime threshold, int batchSize, CancellationToken ct = default)
        => _context.SupportTickets
            .AsNoTracking()
            .Where(t => t.SlaBreachAt <= threshold
                     && t.Status != TicketStatus.Resolved
                     && t.Status != TicketStatus.Closed)
            .OrderBy(t => t.SlaBreachAt)
            .Take(batchSize)
            .ToListAsync(ct)
            .ContinueWith(task => (IReadOnlyList<SupportTicket>)task.Result, ct);
}
