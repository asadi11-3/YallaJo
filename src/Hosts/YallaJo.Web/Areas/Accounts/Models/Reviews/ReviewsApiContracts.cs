namespace YallaJo.Web.Areas.Accounts.Models.Reviews;

// Mirrors the backend ReviewTargetType enum (Social.Domain.Enums).
// The web ApiClient has no JsonStringEnumConverter, but the backend ReviewDto
// returns TargetType as a STRING, so reads are string-based; for writes we send
// the enum which System.Text.Json serializes as a number by default.
public enum ReviewTargetType : byte
{
    Tour = 0,
    Place = 1,
    Business = 2,
    TourGuide = 3,
}

// Maps backend ReviewDto (Social.Application.Queries.Dtos.ReviewDto).
public sealed record ReviewItemResponse(
    Guid Id,
    Guid UserId,
    string TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string Status,
    bool IsVerifiedBooking,
    bool ProfanityFlagged,
    int CurrentReportCount,
    DateTime? LastEditedAt,
    DateTime? AutoHiddenAt,
    DateTime CreatedAt,
    int HelpfulVoteCount,
    string RowVersion,
    IReadOnlyList<ReviewReplyResponse> Replies);

public sealed record ReviewReplyResponse(
    Guid Id,
    Guid ReviewId,
    Guid ProviderUserId,
    string Content,
    DateTime CreatedAt,
    DateTime? LastEditedAt);

// Maps backend ReviewPageDto (cursor-paged).
public sealed record ReviewPageResponse(
    IReadOnlyList<ReviewItemResponse> Items,
    string? NextCursor);

// POST /api/v1/social/reviews body.
public sealed record CreateReviewRequest(
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate);

// PUT /api/v1/social/reviews/{id} body.
public sealed record EditReviewRequest(
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string? RowVersion);

// DELETE /api/v1/social/reviews/{id} body.
public sealed record DeleteReviewRequest(string? RowVersion);
