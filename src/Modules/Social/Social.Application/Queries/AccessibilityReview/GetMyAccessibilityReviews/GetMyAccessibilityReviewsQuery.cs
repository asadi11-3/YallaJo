using Social.Application.Queries.AccessibilityReview.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.AccessibilityReview.GetMyAccessibilityReviews;

public sealed record GetMyAccessibilityReviewsQuery(
    Guid UserId,
    Guid? AfterId,
    int PageSize) : IQuery<AccessibilityReviewPageDto>;
