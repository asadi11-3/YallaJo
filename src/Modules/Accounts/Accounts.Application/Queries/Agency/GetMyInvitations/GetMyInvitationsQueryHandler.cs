using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetMyInvitations;

public sealed class GetMyInvitationsQueryHandler(
    IAgencyInvitationRepository agencyInvitationRepository,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<GetMyInvitationsQueryHandler> logger)
    : IQueryHandler<GetMyInvitationsQuery, IReadOnlyList<InvitationDto>>
{
    public async Task<Result<IReadOnlyList<InvitationDto>>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var invitations = await cache.GetOrCreateAsync(
            AccountsCacheKeys.AgencyInvitations(userId, request.Direction),
            async ct =>
            {
                var list = request.Direction == "sent"
                    ? await agencyInvitationRepository.GetByAgencyUserIdAsync(userId, statusFilter: null, ct)
                    : await agencyInvitationRepository.GetByGuideUserIdAsync(userId, statusFilter: AgencyInvitationStatus.Pending, ct);

                return list
                    .Select(i => new InvitationDto(
                        i.Id, i.AgencyUserId, i.GuideUserId, i.Message,
                        i.ProposedCommissionPercentage, i.Status,
                        i.ExpiresAt, i.RespondedAt, i.CreatedAt))
                    .ToList()
                    .AsReadOnly() as IReadOnlyList<InvitationDto>;
            },
            tags: [AccountsCacheKeys.AgencyInvitationsTag(userId)],
            cancellationToken: cancellationToken);

        return Result<IReadOnlyList<InvitationDto>>.Success(invitations!);
    }
}
