using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.LeaveAgency;

public sealed class LeaveAgencyCommandHandler(
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<LeaveAgencyCommandHandler> logger)
    : ICommandHandler<LeaveAgencyCommand>
{
    public async Task<Result> Handle(LeaveAgencyCommand request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;

        var affiliation = await agencyAffiliationRepository.GetActiveByGuideUserIdAsync(guideUserId, cancellationToken);
        if (affiliation is null)
            return Result.Failure(AgencyErrors.NoActiveAffiliation, Outcome.NotFound);

        affiliation.Terminate(guideUserId, "Guide voluntarily left the agency.");

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("AgencyAffiliation.ConcurrencyConflict", "The agency affiliation was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyGuidesTag(affiliation.AgencyUserId), cancellationToken);

        logger.LogInformation("Guide {GuideUserId} left agency {AgencyUserId}", guideUserId, affiliation.AgencyUserId);

        return Result.Success();
    }
}
