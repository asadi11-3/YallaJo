using ContentCore.Contracts.Attachments;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Common;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveReview;

internal sealed class RemoveReviewCommandHandler(
    IReviewRepository reviewRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    IEntityAttachmentCleanupService attachmentCleanup,
    ILogger<RemoveReviewCommandHandler> logger)
    : IRequestHandler<RemoveReviewCommand, Result>
{
    private const string ReviewAttachmentEntityType = "Review";


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

        if (!RowVersionUtil.Equal(review.RowVersion, request.RowVersion))
            return Result.Failure(
                new Error("Review.ConcurrencyConflict", "This review was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

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
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.UserReviewsTag(review.UserId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(request.ReviewId), ct).ConfigureAwait(false);

        // Best-effort cleanup of the review's public images; never fail the removal.
        try
        {
            await attachmentCleanup
                .DeleteEntityAttachmentsAsync(ReviewAttachmentEntityType, review.Id, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up images for removed review {ReviewId}", request.ReviewId);
        }

        logger.LogInformation("Admin {AdminId} removed review {ReviewId}", request.AdminUserId, request.ReviewId);
        return Result.Success();
    }
}
