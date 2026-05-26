using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlockEntity = ContentTours.Domain.Entities.GuideAvailabilityBlock;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Create;

internal sealed class CreateGuideAvailabilityBlockCommandHandler(
    IGuideAvailabilityBlockRepository repository,
    ITourGuideRepository guideRepository,
    IContentToursUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<CreateGuideAvailabilityBlockCommandHandler> logger) : ICommandHandler<CreateGuideAvailabilityBlockCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGuideAvailabilityBlockCommand request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
            return Result<Guid>.Failure(
                new Error("GuideAvailabilityBlock.InvalidDateRange", "StartDate must be on or before EndDate."),
                Outcome.Invalid);

        var guideUserId = currentUser.UserId!.Value;
        var guide = await guideRepository.GetByUserIdAsync(guideUserId, cancellationToken, asNoTracking: false);
        if (guide is null)
            return Result<Guid>.Failure(new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);

        var blockResult = BlockEntity.Create(guide.Id, request.StartDate, request.EndDate, request.Reason);
        if (blockResult.IsFailure)
            return Result<Guid>.Failure(blockResult.Error, Outcome.Invalid);

        var block = blockResult.Value;
        repository.Add(block);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict creating availability block for GuideId={GuideId}", guide.Id);
            return Result<Guid>.Failure(new Error("GuideAvailabilityBlock.ConcurrencyConflict", "The guide availability was modified by another request. Please retry."), Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForAvailabilityBlocks(guideUserId), cancellationToken);
        logger.LogInformation("Created availability block {BlockId} for GuideId={GuideId}", block.Id, guide.Id);
        return Result.Success(block.Id);
    }
}

