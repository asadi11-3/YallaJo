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

namespace Social.Application.Commands.ApproveReview;

internal sealed class ApproveReviewCommandHandler(
    IReviewRepository reviewRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<ApproveReviewCommandHandler> logger)
    : IRequestHandler<ApproveReviewCommand, Result>
{
    public async Task<Result> Handle(ApproveReviewCommand request, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, ct);
        if (review is null)
        {
            return Result.Failure(
                new Error("Review.NotFound", $"Review {request.ReviewId} not found."),
                Outcome.NotFound);
        }

        if (review.Status is ReviewStatus.Published)
        {
            return Result.Failure(
                new Error("Review.AlreadyPublished", "This review is already published."),
                Outcome.Conflict);
        }

        if (review.Status is ReviewStatus.DeletedByUser or ReviewStatus.RemovedByAdmin)
        {
            return Result.Failure(
                new Error("Review.AlreadyDeleted", "A deleted review cannot be approved."),
                Outcome.Conflict);
        }

        if (!RowVersionUtil.Equal(review.RowVersion, request.RowVersion))
            return Result.Failure(
                new Error("Review.ConcurrencyConflict", "This review was modified by another user. Please refresh and try again."),
                Outcome.Conflict);

        review.Restore(request.AdminUserId, timeProvider);

        var log = ContentModerationLog.Create(
            request.AdminUserId,
            ReportableEntityType.Review,
            review.Id,
            ModerationAction.RestoreContent,
            request.Notes,
            timeProvider.GetUtcNow().UtcDateTime);

        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(review.TargetType, review.TargetId), ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewTag(request.ReviewId), ct).ConfigureAwait(false);

        logger.LogInformation("Admin {AdminId} approved review {ReviewId}", request.AdminUserId, request.ReviewId);
        return Result.Success();
    }
}
