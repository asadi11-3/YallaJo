namespace Accounts.Application.Caching;

public static class AccountsCacheKeys
{
    public static string UserProfile(Guid userId) => $"accounts:profile:{userId}";
    public static string UserProfileTag(Guid userId) => $"accounts:profile:{userId}";
}
