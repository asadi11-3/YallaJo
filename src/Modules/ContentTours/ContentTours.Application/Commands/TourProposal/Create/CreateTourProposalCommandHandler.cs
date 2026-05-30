using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourProposal.Create;

public sealed class CreateTourProposalCommandHandler(
    ITourGuideRepository guideRepository,
    ITourProposalRepository proposalRepository,
    IPlaceExistenceService placeExistenceService,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourProposalCommandHandler> logger)
    : ICommandHandler<CreateTourProposalCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTourProposalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;

            var guide = await guideRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (guide is null)
            {
                return Result<Guid>.Failure(new Error("TourGuide.NotFound", "You do not have a tour guide profile."), Outcome.NotFound);
            }

            // Validate place exists
            var placeStatus = await placeExistenceService.GetStatusAsync(request.PlaceId, cancellationToken).ConfigureAwait(false);
            if (placeStatus == PlaceExistenceStatus.NotFound)
            {
                return Result<Guid>.Failure(new Error("Place.NotFound", $"Place '{request.PlaceId}' not found."), Outcome.NotFound);
            }

            if (placeStatus == PlaceExistenceStatus.Deleted)
            {
                return Result<Guid>.Failure(new Error("Place.Deleted", $"Place '{request.PlaceId}' has been deleted."), Outcome.Conflict);
            }

            var proposalResult = Domain.Entities.TourProposal.Create(
                guide.Id,
                userId,
                request.Title,
                request.Description,
                request.ShortDescription,
                request.PlaceId,
                request.DurationMinutes,
                request.MaxGroupSize,
                request.BasePrice,
                request.Currency,
                request.RequestExclusive);

            if (!proposalResult.IsSuccess)
            {
                return Result<Guid>.Failure(proposalResult.Error!, proposalResult.Outcome);
            }

            await proposalRepository.AddAsync(proposalResult.Value!, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Guid>.Failure(new Error("TourProposal.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideProposals(guide.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour proposal created: {ProposalId} by guide {GuideId}", proposalResult.Value!.Id, guide.Id);
            return Result<Guid>.Success(proposalResult.Value!.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<Guid>.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
