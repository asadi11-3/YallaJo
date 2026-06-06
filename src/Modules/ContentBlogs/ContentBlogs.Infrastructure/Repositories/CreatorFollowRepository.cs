using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorFollowRepository(ContentBlogsDbContext context) : ICreatorFollowRepository
{
    public Task<CreatorFollow?> GetAsync(
        Guid followerUserId,
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorFollows
            .FirstOrDefaultAsync(
                f => f.FollowerUserId == followerUserId && f.CreatorProfileId == creatorProfileId,
                cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid followerUserId,
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorFollows
            .AnyAsync(
                f => f.FollowerUserId == followerUserId && f.CreatorProfileId == creatorProfileId,
                cancellationToken);
    }

    public async Task AddAsync(
        CreatorFollow follow,
        CancellationToken cancellationToken = default)
    {
        await context.CreatorFollows.AddAsync(follow, cancellationToken).ConfigureAwait(false);
    }

    public void Remove(CreatorFollow follow)
    {
        context.CreatorFollows.Remove(follow);
    }

    public Task<int> CountByCreatorProfileIdAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorFollows
            .CountAsync(f => f.CreatorProfileId == creatorProfileId, cancellationToken);
    }

    public async Task<List<Guid>> GetFollowerUserIdsAsync(
        Guid creatorProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorFollows
            .Where(f => f.CreatorProfileId == creatorProfileId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.FollowerUserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<DateTime>> GetFollowerTimestampsAsync(
        Guid creatorProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Public-safe (Gap 3 Phase A): projects only CreatedAt — never the
        // FollowerUserId — so no follower identity can leak through this path.
        return await context.CreatorFollows
            .Where(f => f.CreatorProfileId == creatorProfileId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
