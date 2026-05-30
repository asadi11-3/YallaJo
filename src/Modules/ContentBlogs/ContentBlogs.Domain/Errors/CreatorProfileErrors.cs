using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Creators.CreatorProfile"/>.</summary>
public static class CreatorProfileErrors
{
    public static readonly Error NotFound =
        new("CreatorProfile.NotFound", "Creator profile was not found.");

    public static readonly Error AlreadySuspended =
        new("CreatorProfile.AlreadySuspended", "Creator profile is already suspended.");

    public static readonly Error NotSuspended =
        new("CreatorProfile.NotSuspended", "Creator profile is not suspended.");

    public static readonly Error AlreadyActive =
        new("CreatorProfile.AlreadyActive", "Creator profile is already active.");

    public static readonly Error Suspended =
        new("CreatorProfile.Suspended", "Operation not allowed while the creator profile is suspended.");

    public static readonly Error SlugAlreadyTaken =
        new("CreatorProfile.SlugAlreadyTaken", "The requested slug is already in use by another creator.");

    public static readonly Error AlreadyFollowing =
        new("CreatorProfile.AlreadyFollowing", "You are already following this creator.");

    public static readonly Error NotFollowing =
        new("CreatorProfile.NotFollowing", "You are not following this creator.");

    public static readonly Error CannotFollowSelf =
        new("CreatorProfile.CannotFollowSelf", "You cannot follow yourself.");

    public static readonly Error AlreadyAtOrAboveTier =
        new("CreatorProfile.AlreadyAtOrAboveTier", "Creator is already at or above the target tier.");

    public static readonly Error AlreadyAtOrBelowTier =
        new("CreatorProfile.AlreadyAtOrBelowTier", "Creator is already at or below the target tier.");
}
