using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.AdminDeactivateGuide;

internal sealed class AdminDeactivateTourGuideCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AdminDeactivateTourGuideCommandHandler> logger)
    : ICommandHandler<AdminDeactivateTourGuideCommand>
{
    public async Task<Result> Handle(
        AdminDeactivateTourGuideCommand request,
        CancellationToken cancellationToken)
    {
        var guide = await guideRepository
            .GetByIdAsync(request.TourGuideId, cancellationToken)
            .ConfigureAwait(false);

        if (guide is null)
        {
            return Result.Failure(
                new Error("TourGuide.NotFound", $"Tour guide '{request.TourGuideId}' was not found."),
                Outcome.NotFound);
        }

        var deactivateResult = guide.Deactivate();
        if (deactivateResult.IsFailure)
        {
            return deactivateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken);
        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagAllGuidesList, cancellationToken);

        logger.LogInformation("Admin deactivated tour guide {GuideId}", guide.Id);

        return Result.Success();
    }
}
