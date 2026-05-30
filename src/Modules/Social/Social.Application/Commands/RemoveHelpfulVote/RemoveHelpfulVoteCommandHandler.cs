using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveHelpfulVote;

internal sealed class RemoveHelpfulVoteCommandHandler(
    IReviewRepository reviewRepository,
    IReviewHelpfulVoteRepository voteRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<RemoveHelpfulVoteCommandHandler> logger)
    : ICommandHandler<RemoveHelpfulVoteCommand>
{
    public async Task<Result> Handle(RemoveHelpfulVoteCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
        {
            return Result.Failure(new Error("Review.NotFound", "Review was not found."), Outcome.NotFound);
        }

        var vote = await voteRepository.GetVoteAsync(command.ReviewId, command.UserId, ct);
        if (vote is null)
        {
            return Result.Success();
        }

        voteRepository.RemoveVote(vote);
        review.DecrementHelpfulVotes();
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(review.Id), ct);

        logger.LogInformation("User {UserId} removed helpful vote from review {ReviewId}", command.UserId, command.ReviewId);
        return Result.Success();
    }
}
