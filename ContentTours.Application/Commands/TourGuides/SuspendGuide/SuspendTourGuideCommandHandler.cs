using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.SuspendGuide;

public sealed class SuspendTourGuideCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursOutboxWriter outboxWriter,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SuspendTourGuideCommandHandler> logger)
    : ICommandHandler<SuspendTourGuideCommand>
{
    public async Task<Result> Handle(SuspendTourGuideCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guide = await guideRepository.GetByIdAsync(request.TourGuideId, cancellationToken, asNoTracking: false).ConfigureAwait(false);
            if (guide is null)
            {
                return Result.Failure(new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);
            }

            var adminId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;
            var suspendResult = guide.Suspend(adminId, request.Reason, utcNow);
            if (!suspendResult.IsSuccess)
            {
                return suspendResult;
            }

            outboxWriter.Enqueue(new TourGuideSuspendedIntegrationEvent(
                TourGuideId: guide.Id,
                UserId: guide.UserId,
                Reason: request.Reason,
                SuspendedByAdminId: adminId,
                SuspendedAt: utcNow));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("TourGuide.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour guide {GuideId} suspended by {AdminId}", guide.Id, adminId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
