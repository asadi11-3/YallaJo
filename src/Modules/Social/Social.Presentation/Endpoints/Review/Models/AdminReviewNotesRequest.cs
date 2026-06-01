namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record AdminReviewNotesRequest(string? Notes = null, string? RowVersion = null);
