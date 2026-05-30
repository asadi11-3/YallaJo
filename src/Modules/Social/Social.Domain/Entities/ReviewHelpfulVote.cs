namespace Social.Domain.Entities;

/// <summary>Simple helpful up-vote for a review. Composite key: ReviewId + UserId.</summary>
public sealed class ReviewHelpfulVote
{
    private ReviewHelpfulVote() { } // EF Core

    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime VotedAt { get; private set; }

    public static ReviewHelpfulVote Create(Guid reviewId, Guid userId, TimeProvider timeProvider)
        => new()
        {
            ReviewId = reviewId,
            UserId = userId,
            VotedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
}
