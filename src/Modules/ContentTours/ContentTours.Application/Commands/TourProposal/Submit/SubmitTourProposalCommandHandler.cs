using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourProposal.Submit;

public sealed class SubmitTourProposalCommandHandler(
    ITourProposalRepository proposalRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SubmitTourProposalCommandHandler> logger)
    : ICommandHandler<SubmitTourProposalCommand>
{
    public async Task<Result> Handle(SubmitTourProposalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await proposalRepository.GetWithDetailsAsync(request.ProposalId, cancellationToken).ConfigureAwait(false);
            if (proposal is null)
            {
                return Result.Failure(new Error("TourProposal.NotFound", "Proposal not found."), Outcome.NotFound);
            }

            if (proposal.GuideUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(new Error("TourProposal.NotOwner", "You do not own this proposal."), Outcome.Forbidden);
            }

            var submitResult = proposal.Submit();
            if (!submitResult.IsSuccess)
            {
                return submitResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("TourProposal.ConcurrencyConflict", "Concurrent update detected."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagForGuideProposals(proposal.TourGuideId), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(TourGuideCacheKeys.TagAdminProposalQueue, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Tour proposal {ProposalId} submitted by guide {GuideId}", request.ProposalId, proposal.TourGuideId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
