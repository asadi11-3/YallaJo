using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.UpdateReviewReply;

internal sealed class UpdateReviewReplyCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    ILogger<UpdateReviewReplyCommandHandler> logger)
    : IRequestHandler<UpdateReviewReplyCommand, Result>
{
    public async Task<Result> Handle(UpdateReviewReplyCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
            return Result.Failure(new Error("Review.NotFound", "Review not found."), Outcome.NotFound);

        if (!RowVersionUtil.Equal(review.RowVersion, command.RowVersion))
            return Result.Failure(
                new Error("Review.ConcurrencyConflict", "This review was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        try
        {
            review.UpdateReply(command.ReplyId, command.Content, command.CallerUserId);
        }
        catch (KeyNotFoundException)
        {
            return Result.Failure(new Error("Review.ReplyNotFound", "Reply not found."), Outcome.NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Review.Forbidden", ex.Message), Outcome.Forbidden);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Reply {ReplyId} updated on review {ReviewId} by {CallerUserId}",
            command.ReplyId, command.ReviewId, command.CallerUserId);
        return Result.Success();
    }
}
