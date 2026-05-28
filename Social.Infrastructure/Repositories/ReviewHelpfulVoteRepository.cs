using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Repositories;

internal sealed class ReviewHelpfulVoteRepository(SocialDbContext context) : IReviewHelpfulVoteRepository
{
    public Task<bool> HasVotedAsync(Guid reviewId, Guid userId, CancellationToken ct = default)
        => context.ReviewHelpfulVotes.AnyAsync(x => x.ReviewId == reviewId && x.UserId == userId, ct);

    public async Task AddVoteAsync(ReviewHelpfulVote vote, CancellationToken ct = default)
        => await context.ReviewHelpfulVotes.AddAsync(vote, ct).ConfigureAwait(false);

    public Task<ReviewHelpfulVote?> GetVoteAsync(Guid reviewId, Guid userId, CancellationToken ct = default)
        => context.ReviewHelpfulVotes.FirstOrDefaultAsync(x => x.ReviewId == reviewId && x.UserId == userId, ct);

    public void RemoveVote(ReviewHelpfulVote vote) => context.ReviewHelpfulVotes.Remove(vote);
}
