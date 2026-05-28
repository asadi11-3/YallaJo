using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AddHelpfulVote;

internal sealed class AddHelpfulVoteCommandHandler(
    IReviewRepository reviewRepository,
    IReviewHelpfulVoteRepository voteRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<AddHelpfulVoteCommandHandler> logger)
    : ICommandHandler<AddHelpfulVoteCommand>
{
    public async Task<Result> Handle(AddHelpfulVoteCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
        {
            return Result.Failure(new Error("Review.NotFound", "Review was not found."), Outcome.NotFound);
        }

        if (await voteRepository.HasVotedAsync(command.ReviewId, command.UserId, ct))
        {
            return Result.Failure(new Error("ReviewHelpfulVote.AlreadyVoted", "You already marked this review as helpful."), Outcome.Conflict);
        }

        await voteRepository.AddVoteAsync(ReviewHelpfulVote.Create(command.ReviewId, command.UserId, timeProvider), ct);
        review.IncrementHelpfulVotes();
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(review.Id), ct);

        logger.LogInformation("User {UserId} marked review {ReviewId} helpful", command.UserId, command.ReviewId);
        return Result.Success();
    }
}
