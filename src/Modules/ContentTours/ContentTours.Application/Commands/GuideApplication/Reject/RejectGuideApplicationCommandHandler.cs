using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideApplication.Reject;

public sealed class RejectGuideApplicationCommandHandler(
    IGuideApplicationRepository applicationRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectGuideApplicationCommandHandler> logger)
    : ICommandHandler<RejectGuideApplicationCommand>
{
    public async Task<Result> Handle(RejectGuideApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var application = await applicationRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken).ConfigureAwait(false);
            if (application is null)
            {
                return Result.Failure(new Error("GuideApplication.NotFound", "Application not found."), Outcome.NotFound);
            }

            var adminId = currentUser.UserId!.Value;
            var rejectResult = application.Reject(adminId, request.Reason, DateTime.UtcNow);
            if (!rejectResult.IsSuccess)
            {
                return rejectResult;
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

            logger.LogInformation("Guide application {ApplicationId} rejected by {AdminId}", request.ApplicationId, adminId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
