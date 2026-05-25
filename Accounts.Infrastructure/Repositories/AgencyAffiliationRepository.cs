using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Accounts.Infrastructure.Repositories;

public sealed class AgencyAffiliationRepository(AccountsDbContext context)
    : EfRepository<AgencyAffiliation, Guid>(context), IAgencyAffiliationRepository
{
    public async Task<AgencyAffiliation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.AgencyAffiliations
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<AgencyAffiliation?> GetActiveByGuideUserIdAsync(Guid guideUserId, CancellationToken ct = default)
        => await context.AgencyAffiliations
            .FirstOrDefaultAsync(a => a.GuideUserId == guideUserId && a.Status == AgencyAffiliationStatus.Active, ct);

    public async Task<bool> IsGuideAffiliatedAsync(Guid guideUserId, CancellationToken ct = default)
        => await context.AgencyAffiliations
            .AnyAsync(a => a.GuideUserId == guideUserId && a.Status == AgencyAffiliationStatus.Active, ct);

    public async Task<IReadOnlyList<AgencyAffiliation>> GetByAgencyUserIdAsync(Guid agencyUserId, AgencyAffiliationStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.AgencyAffiliations.Where(a => a.AgencyUserId == agencyUserId);
        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }
}
