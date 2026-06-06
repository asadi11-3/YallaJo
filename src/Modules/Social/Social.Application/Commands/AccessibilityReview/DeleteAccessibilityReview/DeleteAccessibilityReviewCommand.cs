using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.AccessibilityReview.DeleteAccessibilityReview;

public sealed record DeleteAccessibilityReviewCommand(
    Guid ReviewId,
    Guid CallerUserId,
    bool IsAdmin) : ICommand;
