using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IAgencyAffiliationRepository : IRepository<AgencyAffiliation, Guid>
{
    Task<AgencyAffiliation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<AgencyAffiliation?> GetActiveByGuideUserIdAsync(Guid guideUserId, CancellationToken ct = default);

    Task<bool> IsGuideAffiliatedAsync(Guid guideUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AgencyAffiliation>> GetByAgencyUserIdAsync(
        Guid agencyUserId,
        AgencyAffiliationStatus? statusFilter,
        CancellationToken ct = default);
}
