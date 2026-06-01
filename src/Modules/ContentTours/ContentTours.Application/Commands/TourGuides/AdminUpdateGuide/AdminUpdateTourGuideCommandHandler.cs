using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.AdminUpdateGuide;

internal sealed class AdminUpdateTourGuideCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AdminUpdateTourGuideCommandHandler> logger)
    : ICommandHandler<AdminUpdateTourGuideCommand>
{
    public async Task<Result> Handle(
        AdminUpdateTourGuideCommand request,
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

        var updateResult = guide.UpdateProfile(
            request.Bio ?? guide.Bio,
            request.YearsOfExperience ?? guide.YearsOfExperience,
            request.HasFirstAid ?? guide.HasFirstAid,
            request.MoTALicenseNumber ?? guide.MoTALicenseNumber);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("TourGuide.ConcurrencyConflict", "Concurrent update detected."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken);
        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagAllGuidesList, cancellationToken);

        logger.LogInformation("Admin updated tour guide {GuideId}", guide.Id);

        return Result.Success();
    }
}
