using Social.Application.Commands.AccessibilityReview.CreateAccessibilityReview;
using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.AccessibilityReview.Models;

public sealed record CreateAccessibilityReviewRequest(
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv)
{
    public CreateAccessibilityReviewCommand ToCommand(Guid userId) => new(
        userId, TargetType, TargetId, Rating, Title, Content, VisitDate, FeatureTypesCsv);
}

public sealed record UpdateAccessibilityReviewRequest(
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv);
