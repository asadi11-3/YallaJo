using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveReview;

internal sealed class RemoveReviewCommandHandler(
    IReviewRepository reviewRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RemoveReviewCommandHandler> logger)
    : IRequestHandler<RemoveReviewCommand, Result>
{
    public async Task<Result> Handle(RemoveReviewCommand request, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review is null)
        {
            return Result.Failure(
                new Error("Review.NotFound", $"Review {request.ReviewId} not found."),
                Outcome.NotFound);
        }

        if (review.Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin)
        {
            return Result.Failure(
                new Error("Review.AlreadyDeleted", "This review has already been removed."),
                Outcome.Conflict);
        }

        review.Delete(ReviewDeletionSource.Admin, timeProvider);

        var log = ContentModerationLog.Create(
            request.AdminUserId,
            ReportableEntityType.Review,
            review.Id,
            ModerationAction.RemoveContent,
            request.Reason,
            timeProvider.GetUtcNow().UtcDateTime);

        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Admin {AdminId} removed review {ReviewId}", request.AdminUserId, request.ReviewId);
        return Result.Success();
    }
}
