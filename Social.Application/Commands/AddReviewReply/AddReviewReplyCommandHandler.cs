using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AddReviewReply;

internal sealed class AddReviewReplyCommandHandler(
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AddReviewReplyCommandHandler> logger)
    : IRequestHandler<AddReviewReplyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddReviewReplyCommand command, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(command.ReviewId, ct);
        if (review is null)
            return Result.Failure<Guid>(new Error("Review.NotFound", "Review not found."), Outcome.NotFound);

        // Only allow replies to non-deleted reviews
        if (review.Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin)
            return Result.Failure<Guid>(
                new Error("Review.AlreadyDeleted", "Cannot reply to a deleted review."),
                Outcome.Conflict);

        var reply = review.AddReply(command.ProviderUserId, command.Content, timeProvider);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Reply {ReplyId} added to review {ReviewId} by provider {ProviderId}",
            reply.Id, command.ReviewId, command.ProviderUserId);
        return Result.Success(reply.Id);
    }
}
