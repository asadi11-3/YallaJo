using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.AccessibilityReview.UpdateAccessibilityReview;

public sealed record UpdateAccessibilityReviewCommand(
    Guid ReviewId,
    Guid UserId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv) : ICommand;
