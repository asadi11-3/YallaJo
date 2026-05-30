using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.DeleteReviewReply;

internal sealed class DeleteReviewReplyCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    ILogger<DeleteReviewReplyCommandHandler> logger)
    : IRequestHandler<DeleteReviewReplyCommand, Result>
{
    public async Task<Result> Handle(DeleteReviewReplyCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
            return Result.Failure(new Error("Review.NotFound", "Review not found."), Outcome.NotFound);

        // Admin can delete any reply; provider can only delete their own
        // Review.DeleteReply is idempotent (no-op if reply not found)
        review.DeleteReply(command.ReplyId, command.CallerUserId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Reply {ReplyId} deleted from review {ReviewId} by {CallerUserId} (isAdmin={IsAdmin})",
            command.ReplyId, command.ReviewId, command.CallerUserId, command.IsAdmin);
        return Result.Success();
    }
}
