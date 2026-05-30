using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.RemoveGuide;

public sealed class RemoveGuideCommandHandler(
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RemoveGuideCommandHandler> logger)
    : ICommandHandler<RemoveGuideCommand>
{
    public async Task<Result> Handle(RemoveGuideCommand request, CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        var affiliation = await agencyAffiliationRepository.GetActiveByGuideUserIdAsync(request.GuideUserId, cancellationToken);
        if (affiliation is null)
            return Result.Failure(AgencyErrors.NotAffiliated, Outcome.NotFound);

        if (affiliation.AgencyUserId != agencyUserId)
            return Result.Failure(AgencyErrors.NotOwner, Outcome.Forbidden);

        affiliation.Terminate(agencyUserId, request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyGuidesTag(agencyUserId), cancellationToken);

        logger.LogInformation("Agency {AgencyUserId} removed guide {GuideUserId}", agencyUserId, request.GuideUserId);

        return Result.Success();
    }
}
