using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Delete;

internal sealed class DeleteGuideAvailabilityBlockCommandHandler(
    IGuideAvailabilityBlockRepository repository,
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<DeleteGuideAvailabilityBlockCommandHandler> logger) : ICommandHandler<DeleteGuideAvailabilityBlockCommand>
{
    public async Task<Result> Handle(DeleteGuideAvailabilityBlockCommand request, CancellationToken cancellationToken)
    {
        var guideUserId = currentUser.UserId!.Value;
        var guide = await guideRepository.GetByUserIdAsync(guideUserId, cancellationToken);
        if (guide is null)
            return Result.Failure(new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);

        var block = await repository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure(new Error("GuideAvailabilityBlock.NotFound", "Block not found."), Outcome.NotFound);

        if (block.GuideId != guide.Id)
            return Result.Failure(new Error("GuideAvailabilityBlock.Forbidden", "You do not own this block."), Outcome.Forbidden);

        repository.Remove(block);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting availability block {BlockId}", request.BlockId);
            return Result.Failure(new Error("GuideAvailabilityBlock.ConcurrencyConflict", "The availability block was modified by another request. Please retry."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForAvailabilityBlocks(guideUserId), cancellationToken);
        logger.LogInformation("Deleted availability block {BlockId} for GuideId={GuideId}", request.BlockId, guide.Id);
        return Result.Success();
    }
}
