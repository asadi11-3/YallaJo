using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for SupportTicket aggregate queries.</summary>
public interface ISupportTicketRepository : IRepository<SupportTicket, Guid>
{
    Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<SupportTicket> Items, Guid? NextCursor)> GetByUserPagedAsync(
        Guid userId, Guid? afterId, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<SupportTicket> Items, Guid? NextCursor)> GetByAdminPagedAsync(
        TicketStatus? status, TicketCategory? category, Guid? afterId, int pageSize, CancellationToken ct = default);

    Task<SupportTicket?> GetUnassignedOldestAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns per-status ticket counts in a single grouped query.
    /// Powers the admin queue counted tabs.
    /// </summary>
    Task<IReadOnlyDictionary<TicketStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);

    /// <summary>Returns open/in-progress tickets whose SLA deadline has passed.</summary>
    Task<IReadOnlyList<SupportTicket>> GetOverdueSlaTicketsAsync(DateTime threshold, int batchSize, CancellationToken ct = default);
}
