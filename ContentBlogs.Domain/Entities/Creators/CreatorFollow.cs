using ContentBlogs.Domain.Errors;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// Junction entity representing a user following a creator.
/// Hard-deleted on unfollow (no soft delete).
/// Unique constraint: (FollowerUserId, CreatorProfileId).
/// Wave 7 – Content Creator Module.
/// </summary>
public sealed class CreatorFollow : BaseEntity
{
    /// <summary>The user who is following.</summary>
    public Guid FollowerUserId { get; private set; }

    /// <summary>The creator profile being followed.</summary>
    public Guid CreatorProfileId { get; private set; }

    /// <summary>Navigation property.</summary>
    public CreatorProfile CreatorProfile { get; private set; } = null!;

    // EF constructor
    private CreatorFollow() { }

    /// <summary>Creates a new follow relationship.</summary>
    public static Result<CreatorFollow> Create(Guid followerUserId, Guid creatorProfileId)
    {
        if (followerUserId == Guid.Empty)
            return Result<CreatorFollow>.Failure(
                new Error("CreatorFollow.InvalidFollower", "Follower user id cannot be empty."));

        if (creatorProfileId == Guid.Empty)
            return Result<CreatorFollow>.Failure(
                new Error("CreatorFollow.InvalidProfile", "Creator profile id cannot be empty."));

        if (followerUserId == creatorProfileId)
            return Result<CreatorFollow>.Failure(CreatorProfileErrors.CannotFollowSelf);

        return Result<CreatorFollow>.Success(new CreatorFollow
        {
            FollowerUserId = followerUserId,
            CreatorProfileId = creatorProfileId
        });
    }
}
