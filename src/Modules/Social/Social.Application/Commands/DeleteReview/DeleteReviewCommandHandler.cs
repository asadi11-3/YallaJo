using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.DeleteReview;

internal sealed class DeleteReviewCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<DeleteReviewCommandHandler> logger)
    : IRequestHandler<DeleteReviewCommand, Result>
{
    public async Task<Result> Handle(DeleteReviewCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
            return Result.Failure(new Error("Review.NotFound", "Review not found."), Outcome.NotFound);

        // Only the author or an admin may delete
        if (review.UserId != command.CallerUserId && !command.IsAdmin)
            return Result.Failure(new Error("Review.Forbidden", "You can only delete your own reviews."), Outcome.Forbidden);

        if (!RowVersionUtil.Equal(review.RowVersion, command.RowVersion))
            return Result.Failure(
                new Error("Review.ConcurrencyConflict", "This review was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        var source = command.IsAdmin
            ? ReviewDeletionSource.Admin
            : ReviewDeletionSource.User;

        review.Delete(source, timeProvider);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.UserReviewsTag(review.UserId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(command.ReviewId), ct).ConfigureAwait(false);

        logger.LogInformation("Review {ReviewId} deleted (source={Source}) by {CallerUserId}",
            command.ReviewId, source, command.CallerUserId);
        return Result.Success();
    }
}
