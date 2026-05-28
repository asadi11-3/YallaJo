using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using GuideTourOfferingEntity = ContentTours.Domain.Entities.GuideTourOffering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideApplication.Approve;

public sealed class ApproveGuideApplicationCommandHandler(
    IGuideApplicationRepository applicationRepository,
    IGuideTourOfferingRepository offeringRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveGuideApplicationCommandHandler> logger)
    : ICommandHandler<ApproveGuideApplicationCommand>
{
    public async Task<Result> Handle(ApproveGuideApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var application = await applicationRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken).ConfigureAwait(false);
            if (application is null)
            {
                return Result.Failure(new Error("GuideApplication.NotFound", "Application not found."), Outcome.NotFound);
            }

            var adminId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;

            var approveResult = application.Approve(adminId, utcNow);
            if (!approveResult.IsSuccess)
            {
                return approveResult;
            }

            // Check if an offering already exists
            var existingOffering = await offeringRepository.GetByTourAndGuideAsync(application.TourId, application.TourGuideId, cancellationToken).ConfigureAwait(false);
            if (existingOffering is null)
            {
                var offering = GuideTourOfferingEntity.Create(
                    application.TourId,
                    application.TourGuideId,
                    isProposer: false,
                    applicationId: application.Id,
                    assignedByUserId: adminId);
                await offeringRepository.AddAsync(offering, cancellationToken).ConfigureAwait(false);
            }
            else if (existingOffering.Status != GuideOfferingStatus.Active)
            {
                existingOffering.Reinstate();
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("GuideApplication.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourApplications(application.TourId), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideApplications(application.TourGuideId), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(application.TourId), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Guide application {ApplicationId} approved by {AdminId}", request.ApplicationId, adminId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
