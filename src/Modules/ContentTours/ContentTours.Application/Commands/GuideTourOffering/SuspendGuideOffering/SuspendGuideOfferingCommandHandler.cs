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

namespace ContentTours.Application.Commands.GuideTourOffering.SuspendGuideOffering;

internal sealed class SuspendGuideOfferingCommandHandler(
    IGuideTourOfferingRepository offeringRepository,
    ITourGuideRepository tourGuideRepository,
    IContentToursOutboxWriter outboxWriter,
    IContentToursUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<SuspendGuideOfferingCommandHandler> logger) : ICommandHandler<SuspendGuideOfferingCommand>
{
    public async Task<Result> Handle(SuspendGuideOfferingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(new Error("Auth.Unauthorized", "Authentication required."), Outcome.Forbidden);

        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken, asNoTracking: false);
        if (offering is null)
            return Result.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        var guide = await tourGuideRepository.GetByIdAsync(request.TourGuideId, cancellationToken);
        if (guide is null)
            return Result.Failure(new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);

        var result = offering.Suspend(currentUser.UserId!.Value, request.Reason);
        if (result.IsFailure)
            return result;

        outboxWriter.Enqueue(new GuideTourOfferingSuspendedIntegrationEvent(
            OfferingId:        offering.Id,
            TourId:            offering.TourId,
            TourGuideId:       offering.TourGuideId,
            GuideUserId:       guide.UserId,
            Reason:            request.Reason,
            SuspendedByAdminId: currentUser.UserId!.Value,
            SuspendedAt:       DateTime.UtcNow));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("GuideTourOffering.ConcurrencyConflict", "Concurrent update detected."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForTourOfferings(request.TourId), cancellationToken);
        logger.LogInformation("Suspended offering TourId={TourId}, GuideId={GuideId}", request.TourId, request.TourGuideId);
        return Result.Success();
    }
}
