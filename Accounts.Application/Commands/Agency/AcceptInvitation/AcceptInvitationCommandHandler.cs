using Accounts.Application.Caching;
using Accounts.Domain.Entities;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IAgencyInvitationRepository agencyInvitationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<AcceptInvitationCommandHandler> logger)
    : ICommandHandler<AcceptInvitationCommand>
{
    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;

        var invitation = await agencyInvitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return Result.Failure(AgencyErrors.InvitationNotFound, Outcome.NotFound);

        // Verify invitation belongs to this guide
        if (invitation.GuideUserId != guideUserId)
            return Result.Failure(AgencyErrors.NotInvited, Outcome.Forbidden);

        if (invitation.IsExpired)
            return Result.Failure(AgencyErrors.InvitationExpired, Outcome.Conflict);

        if (invitation.Status != Domain.Enums.AgencyInvitationStatus.Pending)
            return Result.Failure(AgencyErrors.InvitationNotPending, Outcome.Conflict);

        // Verify guide is not already affiliated elsewhere
        var alreadyAffiliated = await agencyAffiliationRepository.IsGuideAffiliatedAsync(guideUserId, cancellationToken);
        if (alreadyAffiliated)
            return Result.Failure(AgencyErrors.GuideAlreadyAffiliated, Outcome.Conflict);

        invitation.Accept();

        // Create affiliation
        var affiliation = AgencyAffiliation.Create(invitation.AgencyUserId, guideUserId, invitation.ProposedCommissionPercentage);
        agencyAffiliationRepository.Add(affiliation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(guideUserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(invitation.AgencyUserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyGuidesTag(invitation.AgencyUserId), cancellationToken);

        logger.LogInformation("Guide {GuideUserId} accepted invitation from agency {AgencyUserId}", guideUserId, invitation.AgencyUserId);

        return Result.Success();
    }
}
