using Accounts.Contracts.Abstractions;
using Accounts.Domain.Repositories;

namespace Accounts.Infrastructure.Services;

internal sealed class AgencyAffiliationReadService(IAgencyAffiliationRepository repository) : IAgencyAffiliationReadService
{
    public async Task<AgencyAffiliationCommissionInfo?> GetActiveByGuideUserIdAsync(
        Guid guideUserId,
        CancellationToken cancellationToken = default)
    {
        var affiliation = await repository.GetActiveByGuideUserIdAsync(guideUserId, cancellationToken);
        return affiliation is null
            ? null
            : new AgencyAffiliationCommissionInfo(
                affiliation.Id,
                affiliation.AgencyUserId,
                affiliation.GuideUserId,
                affiliation.CommissionPercentage,
                affiliation.JoinedAt);
    }
}
