using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.RejectGuideApplication;

public sealed class RejectGuideApplicationCommandHandler(
    IAgencyApplicationRepository agencyApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RejectGuideApplicationCommandHandler> logger)
    : ICommandHandler<RejectGuideApplicationCommand>
{
    public async Task<Result> Handle(RejectGuideApplicationCommand request, CancellationToken cancellationToken)
    {
        var agencyUserId = currentUser.UserId!.Value;

        var application = await agencyApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(AgencyErrors.ApplicationNotFound, Outcome.NotFound);

        if (application.AgencyUserId != agencyUserId)
            return Result.Failure(AgencyErrors.NotOwner, Outcome.Forbidden);

        if (application.Status != Domain.Enums.AgencyApplicationStatus.Pending)
            return Result.Failure(AgencyErrors.ApplicationNotPending, Outcome.Conflict);

        application.Reject(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyApplicationsTag(agencyUserId), cancellationToken);

        logger.LogInformation("Agency {AgencyUserId} rejected application {ApplicationId}", agencyUserId, request.ApplicationId);

        return Result.Success();
    }
}
