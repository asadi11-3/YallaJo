using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.AccessibilityReview.CreateAccessibilityReview;

public sealed record CreateAccessibilityReviewCommand(
    Guid UserId,
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv) : ICommand<Guid>;
