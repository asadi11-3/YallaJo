using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.DeclineInvitation;

public sealed class DeclineInvitationCommandHandler(
    IAgencyInvitationRepository agencyInvitationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<DeclineInvitationCommandHandler> logger)
    : ICommandHandler<DeclineInvitationCommand>
{
    public async Task<Result> Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;

        var invitation = await agencyInvitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return Result.Failure(AgencyErrors.InvitationNotFound, Outcome.NotFound);

        if (invitation.GuideUserId != guideUserId)
            return Result.Failure(AgencyErrors.NotInvited, Outcome.Forbidden);

        if (invitation.Status != Domain.Enums.AgencyInvitationStatus.Pending)
            return Result.Failure(AgencyErrors.InvitationNotPending, Outcome.Conflict);

        invitation.Decline();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(guideUserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyInvitationsTag(invitation.AgencyUserId), cancellationToken);

        logger.LogInformation("Guide {GuideUserId} declined invitation {InvitationId}", guideUserId, request.InvitationId);

        return Result.Success();
    }
}
