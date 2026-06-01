using Accounts.Application.Caching;
using Accounts.Domain.Entities;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.ApproveGuideApplication;

public sealed class ApproveGuideApplicationCommandHandler(
    IAgencyApplicationRepository agencyApplicationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ApproveGuideApplicationCommandHandler> logger)
    : ICommandHandler<ApproveGuideApplicationCommand>
{
    public async Task<Result> Handle(ApproveGuideApplicationCommand request, CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        var application = await agencyApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(AgencyErrors.ApplicationNotFound, Outcome.NotFound);

        if (application.AgencyUserId != agencyUserId)
            return Result.Failure(AgencyErrors.NotOwner, Outcome.Forbidden);

        if (application.Status != Domain.Enums.AgencyApplicationStatus.Pending)
            return Result.Failure(AgencyErrors.ApplicationNotPending, Outcome.Conflict);

        var alreadyAffiliated = await agencyAffiliationRepository.IsGuideAffiliatedAsync(application.GuideUserId, cancellationToken);
        if (alreadyAffiliated)
            return Result.Failure(AgencyErrors.GuideAlreadyAffiliated, Outcome.Conflict);

        application.Approve();

        var affiliation = AgencyAffiliation.Create(agencyUserId, application.GuideUserId, 20m);
        agencyAffiliationRepository.Add(affiliation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyApplicationsTag(agencyUserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyGuidesTag(agencyUserId), cancellationToken);

        logger.LogInformation("Agency {AgencyUserId} approved application {ApplicationId} from guide {GuideUserId}",
            agencyUserId, request.ApplicationId, application.GuideUserId);

        return Result.Success();
    }
}
