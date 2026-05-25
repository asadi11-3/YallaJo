using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourGuides.UpdateAvatar;

public sealed class UpdateGuideAvatarCommandHandler(
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateGuideAvatarCommandHandler> logger)
    : ICommandHandler<UpdateGuideAvatarCommand>
{
    public async Task<Result> Handle(UpdateGuideAvatarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var guide = await guideRepository.GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken, asNoTracking: false).ConfigureAwait(false);
            if (guide is null)
            {
                return Result.Failure(new Error("TourGuide.NotFound", "Tour guide profile not found."), Outcome.NotFound);
            }

            guide.UpdateAvatar(request.AvatarUrl);

            try { await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("TourGuide.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForProfile(guide.Id), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
