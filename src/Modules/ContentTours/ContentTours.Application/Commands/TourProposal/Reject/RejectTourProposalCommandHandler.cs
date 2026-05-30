using ContentTours.Application.Caching;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourProposal.Reject;

public sealed class RejectTourProposalCommandHandler(
    ITourProposalRepository proposalRepository,
    IContentToursUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectTourProposalCommandHandler> logger)
    : ICommandHandler<RejectTourProposalCommand>
{
    public async Task<Result> Handle(RejectTourProposalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await proposalRepository.GetWithDetailsAsync(request.ProposalId, cancellationToken).ConfigureAwait(false);
            if (proposal is null)
            {
                return Result.Failure(new Error("TourProposal.NotFound", "Proposal not found."), Outcome.NotFound);
            }

            var adminId = currentUser.UserId!.Value;
            var rejectResult = proposal.Reject(adminId, request.Reason, DateTime.UtcNow);
            if (!rejectResult.IsSuccess)
            {
                return rejectResult;
            }

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

            logger.LogInformation("Tour proposal {ProposalId} rejected by {AdminId}", request.ProposalId, adminId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
