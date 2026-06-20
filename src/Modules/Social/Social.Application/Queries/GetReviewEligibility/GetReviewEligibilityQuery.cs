using Social.Application.Queries.Dtos;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetReviewEligibility;

/// <summary>
/// Returns whether the caller may submit a review for a given target entity:
/// whether they have a recent completed booking (eligible) and whether they already reviewed it.
/// Drives UI gating of the public review form so users are not shown a form the backend will reject.
/// </summary>
public sealed record GetReviewEligibilityQuery(
    Guid UserId,
    ReviewTargetType TargetType,
    Guid TargetId) : IQuery<ReviewEligibilityDto>;
