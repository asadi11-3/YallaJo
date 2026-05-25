using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using TourEntity = ContentTours.Domain.Entities.Tour;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Application.Commands.TourProposal.Approve;

public sealed class ApproveTourProposalCommandHandler(
    ITourProposalRepository proposalRepository,
    ITourRepository tourRepository,
    ITourGuideRepository guideRepository,
    IGuideTourOfferingRepository offeringRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveTourProposalCommandHandler> logger)
    : ICommandHandler<ApproveTourProposalCommand>
{
    public async Task<Result> Handle(ApproveTourProposalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await proposalRepository.GetWithDetailsAsync(request.ProposalId, cancellationToken).ConfigureAwait(false);
            if (proposal is null)
            {
                return Result.Failure(new Error("TourProposal.NotFound", "Proposal not found."), Outcome.NotFound);
            }

            var adminId = currentUser.UserId!.Value;
            var utcNow = DateTime.UtcNow;

            // Create the Tour from proposal
            var guide = await guideRepository.GetByIdAsync(proposal.TourGuideId, cancellationToken).ConfigureAwait(false);
            if (guide is null)
            {
                return Result.Failure(new Error("TourGuide.NotFound", "The proposing guide no longer exists."), Outcome.NotFound);
            }

            var shortId = proposal.TourGuideId.ToString("N")[..8];
            var slug = $"{proposal.Title.ToLowerInvariant().Replace(" ", "-").Replace("'", "")}-{shortId}";
            var tour = TourEntity.Create(
                name: proposal.Title,
                slug: slug,
                difficulty: Difficulty.Moderate,
                durationMinutes: proposal.DurationMinutes,
                maxGroupSize: proposal.MaxGroupSize,
                basePriceAmount: proposal.BasePrice,
                currency: proposal.Currency,
                location: new Location(0m, 0m),
                createdByUserId: guide.UserId,
                placeId: proposal.PlaceId,
                description: proposal.Description,
                shortDescription: proposal.ShortDescription,
                ownershipType: TourOwnershipType.GuideProposed,
                isExclusive: request.IsExclusive,
                proposedByGuideId: proposal.TourGuideId);

            await tourRepository.AddAsync(tour, cancellationToken).ConfigureAwait(false);

            // Approve the proposal with the new tour ID
            var approveResult = proposal.Approve(adminId, tour.Id, utcNow);
            if (!approveResult.IsSuccess)
            {
                return approveResult;
            }

            // Create GuideTourOffering for the proposer
            var offering = GuideTourOffering.Create(
                tour.Id,
                proposal.TourGuideId,
                isProposer: true,
                applicationId: null,
                assignedByUserId: adminId);
            await offeringRepository.AddAsync(offering, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("TourProposal.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagAdminProposalQueue, cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideProposals(proposal.TourGuideId), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagToursList, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour proposal {ProposalId} approved → Tour {TourId} created", request.ProposalId, tour.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
