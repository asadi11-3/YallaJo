namespace Accounts.Application.Caching;

public static class AccountsCacheKeys
{
    public static string UserProfile(Guid userId) => $"accounts:profile:{userId}";

    public static string UserProfileTag(Guid userId) => $"accounts:profile:{userId}:tag";

    public static string MyApplicationStatus(Guid userId) => $"accounts:provider:status:{userId}";

    public static string MyApplicationStatusTag(Guid userId) => $"accounts:provider:status:{userId}:tag";

    public const string AdminProviderQueue = "accounts:admin:provider-queue";

    public const string AdminProviderQueueTag = "accounts:admin:provider-queue:tag";

    public static string AgencyGuides(Guid agencyUserId) => $"accounts:agency:{agencyUserId}:guides";
    public static string AgencyGuidesTag(Guid agencyUserId) => $"accounts:agency:{agencyUserId}:guides:tag";
    public static string AgencyInvitations(Guid userId) => $"accounts:agency:{userId}:invitations";
    public static string AgencyInvitations(Guid userId, string direction) => $"accounts:agency:{userId}:invitations:{direction}";
    public static string AgencyInvitationsTag(Guid userId) => $"accounts:agency:{userId}:invitations:tag";
    public static string AgencyApplications(Guid agencyUserId) => $"accounts:agency:{agencyUserId}:applications";
    public static string AgencyApplicationsTag(Guid agencyUserId) => $"accounts:agency:{agencyUserId}:applications:tag";
    public const string AvailableGuides = "accounts:agency:available-guides";
    public const string AvailableGuidesTag = "accounts:agency:available-guides:tag";
    public const string AgencyList = "accounts:agencies";
    public const string AgencyListTag = "accounts:agencies:tag";
}
