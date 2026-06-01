using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.EditReview;

internal sealed class EditReviewCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<EditReviewCommandHandler> logger)
    : IRequestHandler<EditReviewCommand, Result>
{
    public async Task<Result> Handle(EditReviewCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
            return Result.Failure(new Error("Review.NotFound", "Review not found."), Outcome.NotFound);

        // Ownership check — only the original author may edit
        if (review.UserId != command.CallerUserId)
            return Result.Failure(new Error("Review.Forbidden", "You can only edit your own reviews."), Outcome.Forbidden);

        // Deleted reviews cannot be edited
        if (review.Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin)
            return Result.Failure(new Error("Review.AlreadyDeleted", "Cannot edit a deleted review."), Outcome.Conflict);

        if (!RowVersionUtil.Equal(review.RowVersion, command.RowVersion))
            return Result.Failure(
                new Error("Review.ConcurrencyConflict", "This review was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        // S-R3: 48-hour edit window enforced inside Review.Edit — catch and translate
        try
        {
            review.Edit(command.Rating, command.Title, command.Content, command.VisitDate, timeProvider);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Review.EditWindowExpired")
        {
            return Result.Failure(
                new Error("Review.EditWindowExpired", "The 48-hour edit window has passed."),
                Outcome.UnprocessableEntity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.UserReviewsTag(review.UserId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(command.ReviewId), ct).ConfigureAwait(false);

        logger.LogInformation("Review {ReviewId} edited by user {UserId}", command.ReviewId, command.CallerUserId);
        return Result.Success();
    }
}
