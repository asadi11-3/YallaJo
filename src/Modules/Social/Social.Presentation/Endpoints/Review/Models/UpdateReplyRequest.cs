namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record UpdateReplyRequest(string Content, string? RowVersion);
