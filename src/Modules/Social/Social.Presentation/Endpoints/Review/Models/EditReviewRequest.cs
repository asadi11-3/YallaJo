using Social.Application.Commands.EditReview;

namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record EditReviewRequest(
    decimal Rating,
    string? Title,
    string Content,
        DateOnly? VisitDate,
    string? RowVersion)
{
    internal EditReviewCommand ToCommand(Guid reviewId, Guid userId)
    {
        byte[] rowVersion;
        try
        {
            rowVersion = string.IsNullOrWhiteSpace(RowVersion)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(RowVersion);
        }
        catch (FormatException)
        {
            rowVersion = Array.Empty<byte>();
        }

        return new(reviewId, userId, rowVersion, Rating, Title, Content, VisitDate);
    }
}
