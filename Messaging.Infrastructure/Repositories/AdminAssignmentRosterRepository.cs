using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.Repositories;

internal sealed class AdminAssignmentRosterRepository(MessagingDbContext context)
    : IAdminAssignmentRosterRepository
{
    public async Task<AdminAssignmentRoster?> PickNextAdminAsync(CancellationToken ct = default)
    {
        // Round-robin: pick active, not on leave, oldest last-assigned
        var entry = await context.AdminAssignmentRosters
            .Where(r => r.IsActive && !r.IsOnLeave)
            .OrderBy(r => r.LastAssignedAt)
            .FirstOrDefaultAsync(ct);

        if (entry is not null)
        {
            entry.RecordAssignment(DateTime.UtcNow);
        }
        return entry;
    }

    public async Task AddAsync(AdminAssignmentRoster entry, CancellationToken ct = default)
    {
        context.AdminAssignmentRosters.Add(entry);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AdminAssignmentRoster>> GetActiveAsync(CancellationToken ct = default)
        => await context.AdminAssignmentRosters.AsNoTracking()
            .Where(r => r.IsActive && !r.IsOnLeave)
            .OrderBy(r => r.LastAssignedAt)
            .ToListAsync(ct);
}
