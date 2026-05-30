using Accounts.Application.Caching;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetAgencyApplications;

public sealed class GetAgencyApplicationsQueryHandler(
    IAgencyApplicationRepository agencyApplicationRepository,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<GetAgencyApplicationsQueryHandler> logger)
    : IQueryHandler<GetAgencyApplicationsQuery, IReadOnlyList<AgencyApplicationDto>>
{
    public async Task<Result<IReadOnlyList<AgencyApplicationDto>>> Handle(
        GetAgencyApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        var applications = await cache.GetOrCreateAsync(
            AccountsCacheKeys.AgencyApplications(agencyUserId),
            async ct =>
            {
                var list = await agencyApplicationRepository.GetByAgencyUserIdAsync(agencyUserId, statusFilter: null, ct);
                return list
                    .Select(a => new AgencyApplicationDto(
                        a.Id, a.GuideUserId, a.AgencyUserId, a.Message,
                        a.Status, a.RejectionReason, a.RespondedAt, a.CreatedAt))
                    .ToList()
                    .AsReadOnly() as IReadOnlyList<AgencyApplicationDto>;
            },
            tags: [AccountsCacheKeys.AgencyApplicationsTag(agencyUserId)],
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<AgencyApplicationDto>>.Success(applications!);
    }
}
