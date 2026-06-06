using Social.Application.Queries.AccessibilityReview.Common;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.AccessibilityReview.GetPublicAccessibilityReviews;

public sealed record GetPublicAccessibilityReviewsQuery(
    ReviewTargetType TargetType,
    Guid TargetId,
    int Page,
    int PageSize) : IQuery<PublicAccessibilityReviewPageDto>;
