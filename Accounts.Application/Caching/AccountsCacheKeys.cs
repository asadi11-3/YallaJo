namespace Accounts.Application.Caching;

/// <summary>
/// Single source of truth for all cache-key and tag patterns in the Accounts module.
/// Query handlers use these keys to write; command handlers use the tags to invalidate.
/// </summary>
public static class AccountsCacheKeys
{
    /// <summary>
    /// Cache entry key for the full profile (including phone) of one user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The cache key for the specified user's profile.</returns>
    public static string UserProfile(Guid userId) => $"accounts:profile:{userId}";

    /// <summary>
    /// Tag for all cache entries that belong to one user's profile.
    /// Pass to <c>HybridCache.RemoveByTagAsync</c> from any command that mutates
    /// profile state (UpdateProfile, UpdateAvatar, DeleteAvatar, DeleteProfile, etc.).
    /// </summary>
    public static string UserProfileTag(Guid userId) => $"accounts:profile:{userId}";
}
