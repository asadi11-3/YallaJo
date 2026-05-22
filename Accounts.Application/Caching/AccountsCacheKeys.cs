namespace Accounts.Application.Caching;

public static class AccountsCacheKeys
{
    public static string UserProfile(Guid userId) => $"accounts:profile:{userId}";

    public static string UserProfileTag(Guid userId) => $"accounts:profile:{userId}:tag";

    // Provider Application
    public static string MyApplicationStatus(Guid userId) => $"accounts:provider:status:{userId}";

    public static string MyApplicationStatusTag(Guid userId) => $"accounts:provider:status:{userId}:tag";

    public const string AdminProviderQueue = "accounts:admin:provider-queue";

    public const string AdminProviderQueueTag = "accounts:admin:provider-queue:tag";
}
