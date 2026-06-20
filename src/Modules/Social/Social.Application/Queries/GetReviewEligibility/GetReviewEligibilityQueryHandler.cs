using MediatR;
using Social.Application.Queries.Dtos;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetReviewEligibility;

/// <summary>
/// Computes review eligibility for the caller using the same rules the
/// <c>CreateReviewCommandHandler</c> enforces (S-R1 verified-booking gate, S-R2 one-review-per-target),
/// so the UI can gate the review form instead of letting the backend reject a doomed submission.
/// </summary>
internal sealed class GetReviewEligibilityQueryHandler(
    IReviewRepository reviewRepository,
    IBookingEligibilitySnapshotRepository eligibilityRepository,
    TimeProvider timeProvider)
    : IRequestHandler<GetReviewEligibilityQuery, Result<ReviewEligibilityDto>>
{
    public async Task<Result<ReviewEligibilityDto>> Handle(GetReviewEligibilityQuery request, CancellationToken ct)
    {
        var existing = await reviewRepository.GetByUserAndTargetAsync(
            request.UserId, request.TargetType, request.TargetId, ct);
        var alreadyReviewed = existing is not null;

        var snapshot = await eligibilityRepository.GetAsync(
            request.UserId, request.TargetType, request.TargetId, ct);

        var canReview = snapshot is not null
            && snapshot.IsEligibleForVerifiedReview(timeProvider.GetUtcNow().UtcDateTime, windowDays: 30);

        return Result.Success(new ReviewEligibilityDto(canReview, alreadyReviewed));
    }
}
