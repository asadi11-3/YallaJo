using Accounts.Application.Caching;
using Accounts.Domain.Entities;
using Accounts.Domain.Errors;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Agency.ApplyToAgency;

public sealed class ApplyToAgencyCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAgencyApplicationRepository agencyApplicationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ApplyToAgencyCommandHandler> logger)
    : ICommandHandler<ApplyToAgencyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ApplyToAgencyCommand request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;

        var guideApplication = await providerApplicationRepository.GetByUserIdAsync(guideUserId, cancellationToken);
        if (guideApplication is null || guideApplication.Type != ProviderType.IndependentGuide)
            return Result<Guid>.Failure(AgencyErrors.NotAGuide, Outcome.Forbidden);

        // Verify the caller's own provider account is approved
        if (guideApplication.Status != ProviderApplicationStatus.Approved)
            return Result<Guid>.Failure(AgencyErrors.CallerNotApproved, Outcome.Forbidden);

        // Verify the target agency exists and is an Agency provider
        var targetAgency = await providerApplicationRepository.GetByUserIdAsync(request.AgencyUserId, cancellationToken);
        if (targetAgency is null || targetAgency.Type != ProviderType.Agency)
            return Result<Guid>.Failure(AgencyErrors.AgencyNotFound, Outcome.NotFound);

        // Verify the target agency is approved
        if (targetAgency.Status != ProviderApplicationStatus.Approved)
            return Result<Guid>.Failure(AgencyErrors.AgencyNotApproved, Outcome.UnprocessableEntity);

        // Verify guide is not already affiliated
        var alreadyAffiliated = await agencyAffiliationRepository.IsGuideAffiliatedAsync(guideUserId, cancellationToken);
        if (alreadyAffiliated)
            return Result<Guid>.Failure(AgencyErrors.GuideAlreadyAffiliated, Outcome.Conflict);

        // Verify no pending application to this agency already exists
        var pendingExists = await agencyApplicationRepository.HasPendingApplicationAsync(guideUserId, request.AgencyUserId, cancellationToken);
        if (pendingExists)
            return Result<Guid>.Failure(AgencyErrors.PendingApplicationExists, Outcome.Conflict);

        var application = AgencyApplication.Create(guideUserId, request.AgencyUserId, request.Message);
        agencyApplicationRepository.Add(application);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AgencyApplicationsTag(request.AgencyUserId), cancellationToken);

        logger.LogInformation("Guide {GuideUserId} applied to agency {AgencyUserId}", guideUserId, request.AgencyUserId);

        return Result<Guid>.Success(application.Id);
    }
}
