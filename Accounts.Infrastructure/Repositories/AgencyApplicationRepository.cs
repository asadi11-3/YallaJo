using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class AgencyApplicationRepository(AccountsDbContext context)
    : EfRepository<AgencyApplication, Guid>(context), IAgencyApplicationRepository
{
    public async Task<AgencyApplication?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.AgencyApplications
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AgencyApplication>> GetByAgencyUserIdAsync(Guid agencyUserId, AgencyApplicationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.AgencyApplications.Where(a => a.AgencyUserId == agencyUserId);
        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task<bool> HasPendingApplicationAsync(Guid guideUserId, Guid agencyUserId, CancellationToken ct = default)
        => await context.AgencyApplications
            .AnyAsync(a => a.GuideUserId == guideUserId && a.AgencyUserId == agencyUserId && a.Status == AgencyApplicationStatus.Pending, ct);
}
