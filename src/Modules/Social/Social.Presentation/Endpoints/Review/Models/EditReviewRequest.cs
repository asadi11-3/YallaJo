using Social.Application.Commands.EditReview;

namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record EditReviewRequest(
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate)
{
    internal EditReviewCommand ToCommand(Guid reviewId, Guid userId) =>
        new(reviewId, userId, Rating, Title, Content, VisitDate);
}
