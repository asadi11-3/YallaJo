using Social.Application.Commands.CreateReview;
using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record CreateReviewRequest(
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate)
{
    internal CreateReviewCommand ToCommand(Guid userId) =>
        new(userId, TargetType, TargetId, Rating, Title, Content, VisitDate);
}
