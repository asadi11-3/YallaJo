using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.DeleteReview;

internal sealed class DeleteReviewCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
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

        var source = command.IsAdmin
            ? ReviewDeletionSource.Admin
            : ReviewDeletionSource.User;

        review.Delete(source, timeProvider);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Review {ReviewId} deleted (source={Source}) by {CallerUserId}",
            command.ReviewId, source, command.CallerUserId);
        return Result.Success();
    }
}
