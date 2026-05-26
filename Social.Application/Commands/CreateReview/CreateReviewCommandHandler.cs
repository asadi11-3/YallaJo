using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Contracts.Services;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.CreateReview;

internal sealed class CreateReviewCommandHandler(
    IReviewRepository reviewRepository,
    IBookingEligibilitySnapshotRepository eligibilityRepository,
    IProfanityFilter profanityFilter,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<CreateReviewCommandHandler> logger)
    : IRequestHandler<CreateReviewCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateReviewCommand command, CancellationToken ct)
    {
        // S-R2: One review per (UserId, TargetType, TargetId)
        var existing = await reviewRepository.GetByUserAndTargetAsync(
            command.UserId, command.TargetType, command.TargetId, ct);

        if (existing is not null)
        {
            return Result.Failure<Guid>(
                new Error("Review.DuplicateReview", "You have already reviewed this item."),
                Outcome.Conflict);
        }

        // S-R1: Determine if this is a verified booking review
        var snapshot = await eligibilityRepository.GetAsync(
            command.UserId, command.TargetType, command.TargetId, ct);

        bool isVerifiedBooking = snapshot is not null
            && snapshot.IsEligibleForVerifiedReview(
                timeProvider.GetUtcNow().UtcDateTime, windowDays: 30);

        if (!isVerifiedBooking)
        {
            return Result.Failure<Guid>(
                new Error("Review.BookingVerificationRequired", "Only users with a recent completed booking can review this item."),
                Outcome.Forbidden);
        }

        // S-R4: Profanity check
        bool profanityDetected = profanityFilter.ContainsProfanity(command.Content)
            || (command.Title is not null && profanityFilter.ContainsProfanity(command.Title));

        var review = Review.Create(
            command.UserId,
            command.TargetType,
            command.TargetId,
            command.Rating,
            command.Title,
            command.Content,
            command.VisitDate,
            isVerifiedBooking,
            profanityDetected,
            timeProvider);

        await reviewRepository.AddAsync(review, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.ReviewsTag(command.TargetType, command.TargetId), ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.UserReviewsTag(command.UserId), ct);

        logger.LogInformation(
            "Review {ReviewId} created for target {TargetType}/{TargetId} by user {UserId}. Status={Status}",
            review.Id, command.TargetType, command.TargetId, command.UserId, review.Status);

        return Result.Success(review.Id);
    }
}
