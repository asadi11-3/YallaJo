using Social.Domain.Entities;

namespace Social.Domain.Repositories;

public interface IReviewHelpfulVoteRepository
{
    Task<bool> HasVotedAsync(Guid reviewId, Guid userId, CancellationToken ct = default);
    Task AddVoteAsync(ReviewHelpfulVote vote, CancellationToken ct = default);
    Task<ReviewHelpfulVote?> GetVoteAsync(Guid reviewId, Guid userId, CancellationToken ct = default);
    void RemoveVote(ReviewHelpfulVote vote);
}
