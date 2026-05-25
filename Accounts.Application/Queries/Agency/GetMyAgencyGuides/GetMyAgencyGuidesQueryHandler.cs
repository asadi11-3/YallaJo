using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetMyAgencyGuides;

public sealed class GetMyAgencyGuidesQueryHandler(
    IAgencyAffiliationRepository agencyAffiliationRepository,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<GetMyAgencyGuidesQueryHandler> logger)
    : IQueryHandler<GetMyAgencyGuidesQuery, IReadOnlyList<AgencyGuideDto>>
{
    public async Task<Result<IReadOnlyList<AgencyGuideDto>>> Handle(
        GetMyAgencyGuidesQuery request,
        CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        var affiliations = await cache.GetOrCreateAsync(
            AccountsCacheKeys.AgencyGuides(agencyUserId),
            async ct =>
            {
                var list = await agencyAffiliationRepository.GetByAgencyUserIdAsync(agencyUserId, statusFilter: null, ct);
                return list
                    .Where(a => a.Status == AgencyAffiliationStatus.Active)
                    .Select(a => new AgencyGuideDto(a.Id, a.GuideUserId, a.CommissionPercentage, a.CreatedAt))
                    .ToList()
                    .AsReadOnly() as IReadOnlyList<AgencyGuideDto>;
            },
            tags: [AccountsCacheKeys.AgencyGuidesTag(agencyUserId)],
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<AgencyGuideDto>>.Success(affiliations!);
    }
}
