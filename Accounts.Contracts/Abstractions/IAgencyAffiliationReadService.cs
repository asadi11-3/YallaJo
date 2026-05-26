namespace Accounts.Contracts.Abstractions;

public interface IAgencyAffiliationReadService
{
    Task<AgencyAffiliationCommissionInfo?> GetActiveByGuideUserIdAsync(
        Guid guideUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AgencyAffiliationCommissionInfo(
    Guid AffiliationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    decimal CommissionPercentage,
    DateTime JoinedAt);
