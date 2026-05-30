using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class AgencyInvitationRepository(AccountsDbContext context)
    : EfRepository<AgencyInvitation, Guid>(context), IAgencyInvitationRepository
{
    public async Task<AgencyInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.AgencyInvitations
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AgencyInvitation>> GetByAgencyUserIdAsync(Guid agencyUserId, AgencyInvitationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.AgencyInvitations.Where(a => a.AgencyUserId == agencyUserId);
        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgencyInvitation>> GetByGuideUserIdAsync(Guid guideUserId, AgencyInvitationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.AgencyInvitations.Where(a => a.GuideUserId == guideUserId);
        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgencyInvitation>> GetPendingExpiredAsync(DateTime utcNow, CancellationToken ct = default)
        => await context.AgencyInvitations
            .Where(a => a.Status == AgencyInvitationStatus.Pending && a.ExpiresAt <= utcNow)
            .ToListAsync(ct);

    public async Task<bool> HasPendingInvitationAsync(Guid agencyUserId, Guid guideUserId, CancellationToken ct = default)
        => await context.AgencyInvitations
            .AnyAsync(a => a.AgencyUserId == agencyUserId && a.GuideUserId == guideUserId && a.Status == AgencyInvitationStatus.Pending, ct);
}
