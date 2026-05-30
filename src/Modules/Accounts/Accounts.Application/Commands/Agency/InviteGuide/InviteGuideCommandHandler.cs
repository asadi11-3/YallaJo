using Accounts.Application.Caching;
using Accounts.Domain.Entities;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.InviteGuide;

public sealed class InviteGuideCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAgencyInvitationRepository agencyInvitationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<InviteGuideCommandHandler> logger)
    : ICommandHandler<InviteGuideCommand, Guid>
{
    public async Task<Result<Guid>> Handle(InviteGuideCommand request, CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        // Verify caller is an Agency provider
        var agencyApplication = await providerApplicationRepository.GetByUserIdAsync(agencyUserId, cancellationToken);
        if (agencyApplication is null || agencyApplication.Type != Domain.Enums.ProviderType.Agency)
            return Result<Guid>.Failure(AgencyErrors.NotAnAgency, Outcome.Forbidden);

        // Verify the guide is not already affiliated
        var alreadyAffiliated = await agencyAffiliationRepository.IsGuideAffiliatedAsync(request.GuideUserId, cancellationToken);
        if (alreadyAffiliated)
            return Result<Guid>.Failure(AgencyErrors.GuideAlreadyAffiliated, Outcome.Conflict);

        // Verify no pending invitation already exists
        var pendingExists = await agencyInvitationRepository.HasPendingInvitationAsync(agencyUserId, request.GuideUserId, cancellationToken);
        if (pendingExists)
            return Result<Guid>.Failure(AgencyErrors.PendingInvitationExists, Outcome.Conflict);

        var invitation = AgencyInvitation.Create(agencyUserId, request.GuideUserId, request.Message, request.ProposedCommissionPercentage);
        agencyInvitationRepository.Add(invitation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(agencyUserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(request.GuideUserId), cancellationToken);

        logger.LogInformation("Agency {AgencyUserId} invited guide {GuideUserId}", agencyUserId, request.GuideUserId);

        return Result<Guid>.Success(invitation.Id);
    }
}
